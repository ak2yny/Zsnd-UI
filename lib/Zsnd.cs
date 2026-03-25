using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Zsnd_UI.Functions;
using static Zsnd_UI.lib.ZsndProperties;

namespace Zsnd_UI.lib
{
    /// <summary>
    /// A <see cref="FileStream"/> reader, using <paramref name="input"/>, that is machine and source endian aware (<see cref="NeedReverse"/>).
    /// </summary>
    public sealed partial class ZsndReader(FileStream input) : IDisposable
    {
        private readonly FileStream _stream = input;
        public bool NeedReverse { get; private set; }

        /// <summary>
        /// Dispose the underlying <see cref="BufferedStream"/> <see cref="_stream"/> (leaves original <see cref="FileStream"/> open).
        /// </summary>
        public void Dispose() { _stream.Close(); }
        // Note: if e.g. isBE == BitConverter.IsLittleEndian: (_reverse ? BinaryPrimitives.ReverseEndianness) == (isBE ? BinaryPrimitives.ReadUInt16BigEndian), but we use _reverse for arrays as well
        /// <summary>
        /// Reads a machine/source endian aware <see cref="ZsndHeader"/> from the <see cref="_stream"/> and advances the position of the stream by <see cref="Cmd.HeaderCount"/>. (Sets <see cref="NeedReverse"/>, depending on the identified platform.)
        /// </summary>
        /// <returns>The <see cref="ZsndHeader"/> and <see cref="ZsndPlatform"/> read.</returns>
        /// <exception cref="InvalidDataException">If file ID isn't 'ZSND'.</exception>
        public (ZsndHeader Header, ZsndPlatform Platform) ReadHeader()
        {
            Span<uint> uint32 = stackalloc uint[Cmd.HeaderCount];
            Span<byte> buffer = MemoryMarshal.AsBytes(uint32);
            _stream.ReadExactly(buffer);
            ref ZsndHeader header = ref MemoryMarshal.Cast<uint, ZsndHeader>(uint32)[0];
            if (header.Magic != SoundIDs.ZSND) { throw new InvalidDataException($"Missing ZSND I.D. (0x{header.Magic:X8}) in '{_stream.Name}'."); }
            ZsndPlatform plat = new(header.Platform);
            if (NeedReverse = plat.Is7thGen == BitConverter.IsLittleEndian)
            {
                Span<uint> nonmagic = uint32[2..];
                BinaryPrimitives.ReverseEndianness(nonmagic, nonmagic);
            }
            return (Header: header, Platform: plat);
        }
        /// <summary>
        /// Reads a machine/source endian aware 2-byte unsigned integer from the <see cref="_stream"/> and advances the position of the stream by two bytes. (Depending on <see cref="_reverse"/>.)
        /// </summary>
        //public ushort ReadUInt16()
        //{
        //    return NeedReverse ?
        //        BinaryPrimitives.ReverseEndianness(MemoryMarshal.Read<ushort>(InternalRead(stackalloc byte[2]))) :
        //        MemoryMarshal.Read<ushort>(InternalRead(stackalloc byte[2]));
        //}
        /// <summary>
        /// Reads a machine/source endian aware 4-byte unsigned integer from the <see cref="_stream"/> and advances the position of the stream by four bytes. (Depending on <see cref="NeedReverse"/>.)
        /// </summary>
        //public uint ReadUInt32()
        //{
        //    return NeedReverse ?
        //        BinaryPrimitives.ReverseEndianness(MemoryMarshal.Read<uint>(InternalRead(stackalloc byte[4]))) :
        //        MemoryMarshal.Read<uint>(InternalRead(stackalloc byte[4]));
        //}
        /// <summary>
        /// Reads a machine/source endian aware block of 2-byte unsigned integers from the <see cref="_stream"/> and advances the position of the stream by the bytes read (2 * <paramref name="size"/>). (Depending on <see cref="NeedReverse"/>.)
        /// </summary>
        //public Span<ushort> ReadUInt16Span(int size)
        //{
        //    Span<ushort> result = ReadArray<ushort>(size);
        //    if (NeedReverse) { BinaryPrimitives.ReverseEndianness(result, result); }
        //    return result;
        //}
        /// <summary>
        /// Reads a machine/source endian aware block of 4-byte unsigned integers from the <see cref="_stream"/> and advances the position of the stream by the bytes read (4 * <paramref name="size"/>). (Depending on <see cref="NeedReverse"/>.)
        /// </summary>
        public Span<uint> ReadUInt32Span(int size)
        {
            Span<uint> result = ReadArray<uint>(size);
            if (NeedReverse) { BinaryPrimitives.ReverseEndianness(result, result); }
            return result;
        }
        /// <summary>
        /// Reads a block of bytes from the <see cref="_stream"/> and advances the position of the stream by the bytes read (sizeof(<see cref="{T}"/>) * <paramref name="size"/>).
        /// </summary>
        public T[] ReadArray<T>(int size) where T : struct
        {
            T[] result = new T[size];
            Span<byte> buffer = MemoryMarshal.AsBytes(result.AsSpan());
            _stream.ReadExactly(buffer);
            return result;
        }
        /// <summary>
        /// Read the <paramref name="bytes"/> as UTF8 <see cref="string"/> from <paramref name="offset"/> for 64 bytes.
        /// </summary>
        public static string ReadString(Span<byte> bytes, int offset)
        {
            int i = bytes.Slice(offset, 64).IndexOf((byte)0);
            return Encoding.UTF8.GetString(bytes.Slice(offset, i == -1 ? 64 : i)).TrimEnd();
        }
    }
    /// <summary>
    /// A <see cref="FileStream"/> writer, using <paramref name="input"/>, that is machine and source endian aware (<see cref="NeedReverse"/>)
    /// </summary>
    public partial class ZsndWriter(FileStream input, bool be) : IAsyncDisposable
    {
        private readonly Dictionary<string, Microsoft.Win32.SafeHandles.SafeFileHandle> _cache = [];
        private readonly FileStream _stream = input;
        // Could use parallel writes with SafeFileHandle and RandomAccess.WriteAsync on _stream (makin all writes async)
        public bool NeedReverse { get; } = be == BitConverter.IsLittleEndian;

        /// <summary>
        /// Disposes the <see cref="_cache"/>d file handles (but leaves async source <see cref="_stream"/> open).
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            foreach (Microsoft.Win32.SafeHandles.SafeFileHandle handle in _cache.Values) { handle.Dispose(); }
            _cache.Clear();
            GC.SuppressFinalize(this);
        }
        /// <summary>
        /// Writes a <see cref="ZsndHeader"/> <paramref name="header"/> to this stream. The current position of the stream is advanced by the <see cref="Cmd.HeaderSize"/> (100). (Reverts endianness, if <see cref="NeedReverse"/>.)
        /// </summary>
        public void Write(ZsndHeader header)
        {
            Span<byte> headerBytes = MemoryMarshal.AsBytes(MemoryMarshal.
                CreateSpan(ref System.Runtime.CompilerServices.Unsafe.AsRef(in header), 1));
            if (NeedReverse)
            {
                Span<uint> nonmagic = MemoryMarshal.Cast<byte, uint>(headerBytes[8..]);
                BinaryPrimitives.ReverseEndianness(nonmagic, nonmagic);
            }
            _stream.Write(headerBytes);
        }
        /// <summary>
        /// Writes a <paramref name="buffer"/> to this stream. The current position of the stream is advanced by the buffer length.
        /// </summary>
        public void Write(Span<byte> buffer) { _stream.Write(buffer); }
        /// <summary>
        /// Writes a <see cref="uint"/> <paramref name="span"/> to this stream. The current position of the stream is advanced by span.Length * four. (Reverts endianness, if <see cref="NeedReverse"/>.)
        /// </summary>
        public void Write(Span<uint> span)
        {
            if (NeedReverse) { BinaryPrimitives.ReverseEndianness(span, span); }
            Span<byte> buffer = MemoryMarshal.Cast<uint, byte>(span);
            _stream.Write(buffer);
        }
        /// <summary>
        /// Writes the <paramref name="Hashes"/> to this stream, using the <paramref name="Prefix"/>. The current position of the stream is advanced by Hashes.Length * eight.
        /// </summary>
        /// <remarks>Converts hashes to <see cref="uint"/> and sorts + indexes them. (Reverts endianness, if <see cref="NeedReverse"/>.)</remarks>
        public void Write(string?[] Hashes, string Prefix)
        {
            Span<byte> buffer = new byte[Hashes.Length * 8];
            Span<HashPair> HashTable = MemoryMarshal.Cast<byte, HashPair>(buffer);
            for (int i = 0; i < Hashes.Length; i++)
            { if (Hashes[i] is not null) { HashTable[i] = new HashPair { Hash = Hashing.PJW($"{Prefix}{Hashes[i]}"), Index = (uint)i }; } }
            HashTable.Sort(static (x, y) => x.Hash.CompareTo(y.Hash));
            if (NeedReverse)
            {
                Span<uint> uintview = MemoryMarshal.Cast<byte, uint>(buffer);
                BinaryPrimitives.ReverseEndianness(uintview, uintview);
            }
            _stream.Write(buffer);
        }
        /// <summary>
        /// Writes a file according to <paramref name="SampleInfo"/> to this stream. The current position of the stream is advanced by the bytes written.
        /// </summary>
        public Task<uint> WriteSampleAsync(UISample SampleInfo, string OutPath, ZsndPlatform Plat, CancellationToken Token)
        {
            return SampleInfo.File is null || SoundIDs.InMagicOf(SampleInfo.File)
                ? WriteFileAsync(Path.Combine(OutPath, SampleInfo.Name!), SampleInfo, Plat, Token)
                : WriteZsndFileAsync(SampleInfo.File, (int)SampleInfo.Offset, SampleInfo.Size, Token);
        }

        private async Task<uint> WriteFileAsync(string SourcePath, UISample SampleInfo, ZsndPlatform Plat, CancellationToken Token)
        {
            if (Plat.IsHeaderless)
            {
                string headerless = ZsndPath.GetHeaderlessPath(SourcePath);
                if (File.Exists(headerless)) { SourcePath = headerless; }
                else if (ZsndConvert.From(Path.GetExtension(SourcePath), SourcePath, SampleInfo, Plat)
                    is Span<byte> ConvertedFileBuffer && ConvertedFileBuffer.Length != 0) // "RIFF"
                {
                    // Audible quality loss, if the files are from old versions with the old decompression method
                    // Only for backwards compatibility.
                    Write(ConvertedFileBuffer);
                    return (uint)ConvertedFileBuffer.Length;
                }
            }
            await using FileStream ss = new(SourcePath, FileMode.Open, FileAccess.Read, FileShare.Read,
                0x1000, FileOptions.Asynchronous | FileOptions.SequentialScan);
            await ss.CopyToAsync(_stream, Token).ConfigureAwait(false);
            return (uint)ss.Length; // Means that individual files can't be more than 4gb (game limit)
        }

        private async Task<uint> WriteZsndFileAsync(string SourcePath, int Offset, uint Size, CancellationToken Token)
        {
            if (!_cache.TryGetValue(SourcePath, out Microsoft.Win32.SafeHandles.SafeFileHandle? source))
            {
                source = File.OpenHandle(SourcePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                _cache[SourcePath] = source;
            }
            await Extensions.CopyToAsync(source, _stream, Offset, (int)Size, Token);
            return Size;
        }
    }
    /// <summary>
    /// Zsnd header <see langword="struct"/> for parsing and writing the header data.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct ZsndHeader(uint InfoSize, uint SoundCount, uint SampleCount)
    {
        public uint Magic = SoundIDs.ZSND;
        public uint Platform;
        public uint Size;
        public uint HeaderSize = InfoSize;
        public uint SoundCount = SoundCount;
        public uint SoundHashesOffset;
        public uint SoundsOffset;
        public uint SampleCount = SampleCount;
        public uint SampleHashesOffset;
        public uint SamplesOffset;
        public uint SampleFileCount = SampleCount;
        public uint SampleFileHashesOffset;
        public uint SampleFilesOffset;
        public uint PhraseCount; // 0
        public uint PhraseHashesOffset = InfoSize;
        public uint PhrasesOffset = InfoSize;
        public uint TrackDefCount; // 0
        public uint TrackDefHashesOffset = InfoSize;
        public uint TrackDefsOffset = InfoSize;
        public uint ReservedCount; // 0
        public uint ReservedHashesOffset = InfoSize;
        public uint ReservedOffset = InfoSize;
        public uint KeymapCount; // 0
        public uint KeymapHashesOffset = InfoSize;
        public uint KeymapsOffset = InfoSize;
    }
    /// <summary>
    /// Uint pair <see langword="struct"/> for temporary hash arrays that are sortable.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct HashPair
    {
        public uint Hash;
        public uint Index;
    }
    /*
    /// <summary>
    /// Zsnd UI Lists
    /// </summary>
    public static class Lists
    {
        public static List<UISound> Sounds { get; set; } = [];
        public static ObservableCollection<JsonSample> Samples { get; set; } = [];
        public static HashSet<string?> XVInternalNames { get; set; } = [];
    }
    /// <summary>
    /// The main bind class, NOTE: Needed when lists are shared among pages.
    /// </summary>
    public class Zsnd
    {
    }
    */
    public static class Cmd
    {
        private static readonly JsonSerializerOptions JsonOptionsD = new() { PropertyNameCaseInsensitive = true };
        private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true, IndentSize = 4, PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };
        public static readonly int HeaderCount = Marshal.SizeOf<ZsndHeader>() / sizeof(uint);
        public static readonly uint HeaderSize = (uint)Marshal.SizeOf<ZsndHeader>();

        /// <summary>
        /// Load the <paramref name="JsonPath"/> as <see cref="UIRoot"/>.
        /// </summary>
        /// <remarks>Exceptions: System.IO; JSON.</remarks>
        public static async Task<UIRoot> LoadJson(string JsonPath, FileEvents FE, CancellationToken Token)
        {
            using FileStream fs = File.OpenRead(JsonPath);
            JsonRoot? JSON = await JsonSerializer.DeserializeAsync<JsonRoot>(fs, JsonOptionsD, Token);
            return JSON is null ? new() : new(JSON, FE); // runs sync
        }
        /// <summary>
        /// Save the lists in <paramref name="Root"/> as a Raven-Formats zsnd json to <paramref name="JsonPath"/>.
        /// </summary>
        /// <remarks>Exceptions: System.IO; JSON.</remarks>
        public static async Task SaveJson(string JsonPath, UIRoot Root, CancellationToken Token)
        {
            _ = ZsndPath.CreateDirectory(JsonPath);
            using FileStream fs = File.OpenWrite(JsonPath);
            await JsonSerializer.SerializeAsync(fs, new JsonRoot(Root), JsonOptions, Token);
        }
        /*
        /// <summary>
        /// Read x_voice info from <paramref name="JsonPath"/>.
        /// </summary>
        public static ZsndPlatform? ReadXVoice(string JsonPath)
        {
            // exceptions unhandled
            if (LoadJson(JsonPath) is JsonRoot XV)
            {
                // Observablec. don't have AddRange (yet). Planned in .Net future versions.
                // Best unofficial extension, performance-wise (no reflection):
                // https://stackoverflow.com/questions/670577/observablecollection-doesnt-support-addrange-method-so-i-get-notified-for-each/45364074#45364074
                Hashing.GlobalFileEvents.Update(Path.GetFileNameWithoutExtension(JsonPath).ToUpperInvariant());
                Lists.XVInternalNames.Clear();
                Lists.Samples = new(XV.Samples);
                Lists.Sounds = [.. XV.Sounds];
                return new(XV.Platform);
            }
            return null;
        }
        /// <summary>
        /// Incomplete: Read sound info from <paramref name="JsonPath"/>.
        /// </summary>
        public static ZsndPlatform? ReadJson(string JsonPath)
        {
            // exceptions unhandled
            if (LoadJson(JsonPath) is JsonRoot ZJ)
            {
                // Should we ensure hashes here?
                //SetPlatform(ZJ.Platform);
                Lists.Samples = new(ZJ.Samples);
                //if (Zname.StartsWith("X_VOICE"))
                //    Lists.XVSounds = [.. ZJ.Sounds];
                //else
                Lists.Sounds = [.. ZJ.Sounds];
                return new(ZJ.Platform);
            }
            return null;
        }
        /// <summary>
        /// [Unused] Read sound info from a .zss/.zsm file, as defined by <paramref name="Zfile"/>, and write sound files to <paramref name="OutPath"/> (defaults to <paramref name="Zfile"/>'s directory).
        /// </summary>
        /// <returns><see langword="True"/>, if parsed; otherwise, <see langword="false"/>.</returns>
        public static ZsndPlatform? LoadZsnd(string Zfile, string OutPath = "")
        {
            List<JsonSample> Samples = [];
            Lists.Sounds.Clear();
            try
            {
                if (LoadZsnd(Zfile, OutPath, Lists.Sounds, Samples) is ZsndPlatform Platform)
                {
                    Lists.Samples = new(Samples);
                    return Platform;
                }
            }
            catch // (Exception e)
            {
                // Handle exceptions here
            }
            return null;
        }
        */
        /// <summary>
        /// Read sound info from a ZSND file, as defined by <paramref name="Zfile"/>.
        /// </summary>
        /// <remarks>Can encounter a multitude of exceptions.</remarks>
        /// <returns>The ZSND properties as <see cref="UIRoot"/>.</returns>
        public static UIRoot LoadZsnd(string Zfile, FileEvents FE, CancellationToken Token)
        {
            // WIP: possibly improve performance by making (but is already wrapped in async?)
            using FileStream fs = new(Zfile, FileMode.Open, FileAccess.Read, FileShare.Read, 0x1000, FileOptions.Asynchronous);
            using ZsndReader reader = new(fs);
            (ZsndHeader Header, ZsndPlatform Plat) = reader.ReadHeader();
            // Sample index is limited by ushort, so int should be sufficient for the spans.
            // Sill, using uint in the struct and cast it here.
            int SampleInfoSz = (int)Plat.SampleInfoSz / 2, SampleInfoSz32 = SampleInfoSz / 2,
                FileInfoSz = (int)Plat.FileInfoSz, FileInfoSz32 = FileInfoSz / 4,
                SampleCount = (int)Header.SampleFileCount, SoundCount = (int)Header.SoundCount;
            // Stream position is only set for safety (except when skipping sample hashes).
            fs.Position = Header.SoundHashesOffset;
            Span<uint> SoundHashes = reader.ReadUInt32Span(SoundCount * 2); fs.Position = Header.SoundsOffset;
            Span<byte> SoundInfo = reader.ReadArray<byte>(SoundCount * 24); fs.Position = Header.SamplesOffset;
            Span<ushort> SoundInfo16 = ToUint16Span(SoundInfo, reader.NeedReverse);
            // skip sample hashes and sample file hashes (can be generated)
            Span<uint> SampleInfo32 = reader.ReadUInt32Span(SampleCount * SampleInfoSz32); fs.Position = Header.SampleFilesOffset;
            Span<ushort> SampleInfo = MemoryMarshal.Cast<uint, ushort>(SampleInfo32); // if _reverse, indices are wrong (see below)
            Span<byte> FileInfo = reader.ReadArray<byte>(SampleCount * FileInfoSz);
            Span<uint> FileInfo32 = ToUint32Span(FileInfo, reader.NeedReverse);

            string Zname = FE.Znames[0];
            int stringO = Plat.ID is Platform.PC ? 12 : 20,
                PSRateO = reader.NeedReverse ? 0 : 1, flagO16 = Plat.IsPS ? reader.NeedReverse ? 3 : 2 : PSRateO; // fix indices because of uint swap
            string Ext = Plat.ID is Platform.GCUB ? "dsp" : "vag";
            UIRoot result = new(Plat, new(SampleCount), new(SoundCount), new bool[SampleCount]);
            for (int i = 0; i < SampleCount; i++)
            {
                // SampleInfo: Skip index (is i)
                int SII = i * SampleInfoSz, FI32I = i * FileInfoSz32;
                result.Samples.Add(new(
                    Plat.IsMicrosoft
                        ? ZsndReader.ReadString(FileInfo, i * FileInfoSz + stringO)
                        : $"{i}.{Ext}",
                    0, // format: FileInfo32[FI32I + 2], // Microsoft only
                    Plat.IsPS
                        ? RoundPS2Rate(SampleInfo[SII + PSRateO] * 44100 / 0x1000)
                        : SampleInfo32[i * SampleInfoSz32 + 1],
                    SampleInfo[SII + flagO16],
                    FileInfo32[FI32I],
                    FileInfo32[FI32I + 1],
                    Zfile
                ));
            }
            uint[] HashTable = new uint[SoundCount];
            for (int i = 0; i < SoundCount; i++) { HashTable[SoundHashes[i * 2 + 1]] = SoundHashes[i * 2]; }
            for (int i = 0; i < SoundCount; i++)
            {
                result.AddSound(HashTable[i], SoundInfo[i * 24 + 6], SoundInfo16[i * 12], FE);
                Token.ThrowIfCancellationRequested(); // because of hash lookup
            }
            return result;
        }

        public static async Task ExtractZsnd(string Zfile, string ExtractDir,
            System.Collections.ObjectModel.ObservableCollection<UISample> samples, bool raw, CancellationToken Token)
        {
            //using FileStream fs = new(Zfile, FileMode.Open, FileAccess.Read, FileShare.Read, 0x1000, FileOptions.Asynchronous);
            //Microsoft.Win32.SafeHandles.SafeFileHandle fh = fs.SafeFileHandle;
            using Microsoft.Win32.SafeHandles.SafeFileHandle fh = File.OpenHandle(Zfile, FileMode.Open, FileAccess.Read, FileShare.Read, FileOptions.Asynchronous);
            string Ext = Path.GetExtension(samples[0].Name!);
            bool nc = raw || Ext.Equals(".dsp", StringComparison.OrdinalIgnoreCase)
                          || Ext.Equals(".xma", StringComparison.OrdinalIgnoreCase);
            byte[] buffer = System.Buffers.ArrayPool<byte>.Shared.Rent(nc ? 0x14000 : 0);
            Memory<byte> mbuffer = buffer;
            try
            {
                for (int i = 0; i < samples.Count; i++)
                {
                    UISample sample = samples[i];
                    if (sample.File is null || sample.Name is null || sample.File != Zfile) { continue; }
                    string TargetPath = Path.Combine(ExtractDir, sample.Name);
                    if (File.Exists(TargetPath)) { continue; }
                    if (raw) { TargetPath = ZsndPath.GetHeaderlessPath(TargetPath); }
                    else { _ = ZsndPath.CreateDirectory(TargetPath); }
                    if (nc)
                    {
                        //await ReadExactlyAsync(fh, buffer.AsMemory(0, (int)sample.Size), sample.Offset, sample.Size, Token).ConfigureAwait(false);
                        using FileStream tfs = new(TargetPath, FileMode.Create, FileAccess.Write, FileShare.None, 0x1000, FileOptions.Asynchronous);
                        uint Offset = sample.Offset;
                        int bytesRemaining = (int)sample.Size, bytesRead = 0; while (bytesRemaining > 0
                            && (bytesRead = await RandomAccess.ReadAsync(fh, mbuffer, Offset, Token).ConfigureAwait(false)) != 0)
                        {
                            // or if (bytesRead == 0) { throw new EndOfStreamException("Unexpected end of source stream"); }
                            await tfs.WriteAsync(new ReadOnlyMemory<byte>(buffer, 0, Math.Min(bytesRead, bytesRemaining)), Token).ConfigureAwait(false);
                            bytesRemaining -= bytesRead; Offset += (uint)bytesRead;
                        }
                    }
                    else { await ZsndConvert.To(sample, fh, Ext, TargetPath, Token); }
                }
            }
            finally { System.Buffers.ArrayPool<byte>.Shared.Return(buffer); }
        }
        /// <summary>
        /// Save the lists in <paramref name="Root"/> as lists as a .zss/.zsm file, as defined by <paramref name="Zfile"/>, according to <paramref name="Root"/>.Platform.
        /// </summary>
        /// <remarks>Resolves file paths that aren't an absolute path, using <paramref name="RelativePath"/> (defaults to <paramref name="Zfile"/>'s directory).</remarks>
        public static async Task WriteZsnd(string Zfile, UIRoot Root, CancellationToken Token, string? RelativePath = null)
        {
            string ZsndName = Path.GetFileNameWithoutExtension(Zfile).ToUpperInvariant();
            RelativePath ??= Path.GetDirectoryName(Zfile)!;
            uint StepSize = 0x10 - 1; // (uint)(Plat.ID == Platform.GCUB ? 0x10 : 0x04)
            uint SoundCount = (uint)Root.Sounds.Count, SampleCount = Math.Min((uint)Root.Samples.Count, ushort.MaxValue + 1);
            int SampleInfoSz = (int)Root.Platform.SampleInfoSz / 2, SampleInfoSz32 = SampleInfoSz / 2,
                FileInfoSz = (int)Root.Platform.FileInfoSz, FileInfoSz32 = FileInfoSz / 4;
            uint soundsOffset = HeaderSize + (SoundCount * 8); // 2 * uint
            uint sampleHOffset = soundsOffset + (SoundCount * 24);
            uint samplesOffset = sampleHOffset + (SampleCount * 8); // 2 * uint
            uint sampleFHOffset = samplesOffset + (SampleCount * Root.Platform.SampleInfoSz);
            uint sampleFOffset = sampleFHOffset + (SampleCount * 8); // 2 * uint
            uint InfoSize = sampleFOffset + (SampleCount * Root.Platform.FileInfoSz); InfoSize = (InfoSize + StepSize) & ~StepSize;

            if (SampleCount > 0x10000) { throw new IndexOutOfRangeException("The maximum file count was exceeded (index is limited by uint16)."); }

            bool IsPC = Root.Platform.ID is Platform.PC;
            uint[] FileInfo32 = new uint[SampleCount * FileInfoSz / 4];
            string?[] FileHashesBase = new string[SampleCount];
            // WIP: It seems like using spans ahead of the loop causes issues
            // Zfile.OpenStreamForWriteAsync(); if Zfile is storage file
            await using FileStream fs = new(Zfile, FileMode.Create, FileAccess.Write, FileShare.None, 0x1000, FileOptions.Asynchronous);
            await using ZsndWriter writer = new(fs, Root.Platform.Is7thGen);
            fs.Position = InfoSize;
            for (int i = 0; i < SampleCount; i++)
            {
                UISample Sa = Root.Samples[i];
                int FI32I = i * FileInfoSz32;
                FileInfo32[FI32I] = (uint)fs.Position; // file can't be more than 4GB, but sizes are waay smaller
                if (Sa.Name is null) { throw new NullReferenceException($"File #{i} is not defined"); }
                // could check with Path.IsPathFullyQualified(S.File)
                uint size = await writer.WriteSampleAsync(Sa, RelativePath, Root.Platform, Token), padding = (uint)(-size & StepSize);
                fs.Seek(padding, SeekOrigin.Current);
                FileInfo32[FI32I + 1] = size + padding;
                if (Root.Platform.IsMicrosoft)
                    FileInfo32[FI32I + 2] = IsPC && (Sa.Flags & SampleF.FourChannels) == SampleF.None ? 106u : 1u;
                else if (Root.Platform.ID == Platform.GCUB)
                    FileInfo32[FI32I + 2] = SoundIDs.DSP;
                FileHashesBase[i] = $"/{ZsndName}/{Path.GetFileNameWithoutExtension(Sa.Name).ToUpperInvariant()}";
            }
            uint Size = (uint)fs.Position; // Might need to assert that size is not more than 4GB
            fs.SetLength(Size); fs.Position = 0;
            Span<uint> FileInfoSpan = FileInfo32;
            if (writer.NeedReverse) { BinaryPrimitives.ReverseEndianness(FileInfoSpan, FileInfoSpan); }
            Span<byte> FileInfosB = MemoryMarshal.Cast<uint, byte>(FileInfoSpan);
            Span<uint> SoundHashes = Hashing.GetRandomizedHashTable(Root.Sounds);
            Span<byte> SoundInfo = new byte[SoundCount * 24];
            Span<ushort> SoundInfo16 = MemoryMarshal.Cast<byte, ushort>(SoundInfo);
            Span<uint> SampleInfo32 = new uint[SampleCount * SampleInfoSz32];
            Span<ushort> SampleInfo = MemoryMarshal.Cast<uint, ushort>(SampleInfo32);
            int stringO = IsPC ? 12 : 20, o1 = Root.Platform.Is7thGen ? 3 : 2;
            if (Root.Platform.IsMicrosoft)
            {
                for (int i = 0; i < SampleCount; i++)
                {
                    ReadOnlySpan<char> name = Path.GetFileName(Root.Samples[i].Name).AsSpan();
                    _ = Encoding.UTF8.GetBytes(name.Length > 64 ? name[..64] : name, FileInfosB[(i * FileInfoSz + stringO)..]);
                }
            }
            for (ushort i = 0; i < SampleCount; i++)
            {
                UISample Sa = Root.Samples[i];
                int SII = i * SampleInfoSz;
                SampleInfo[SII] = i;
                if (Root.Platform.IsPS)
                {
                    SampleInfo[SII + 1] = (ushort)Math.Round(Sa.SampleRate * 0x1000 / 44100.0); // Rate to pitch
                    SampleInfo[SII + 2] = (ushort)Sa.Flags;
                }
                else
                {
                    SampleInfo[SII + 1] = (ushort)Sa.Flags;
                    System.Diagnostics.Debug.Assert(Sa.SampleRate < 0x10000); // assuming SampleRate is max. 0xFFFF
                    SampleInfo[SII + o1] = (ushort)Sa.SampleRate;
                }
            }
            SoundInfo[Root.Platform.Is7thGen ? 2 : 3] = 0x10; // SoundInfo16[1] = 0x1000 depending on endian
            SoundInfo[4] = 0x7F;
            SoundInfo[9] = 0x7F;
            SoundInfo[11] = (byte)(Root.Platform.IsPS ? 0x0F : 0x7F);
            if (Root.Platform.ID == Platform.PS3) { SoundInfo[19] = SoundInfo[20] = SoundInfo[21] = 0x20; }
            for (int i = 24; i < SoundInfo.Length; i += i)
            { SoundInfo[..Math.Min(i, SoundInfo.Length - i)].CopyTo(SoundInfo[i..]); }
            for (int i = 0; i < SoundCount; i++)
            {
                UISound So = Root.Sounds[i];
                SoundInfo16[i * 12] = writer.NeedReverse
                    ? BinaryPrimitives.ReverseEndianness((ushort)So.SampleIndex)
                    : (ushort)So.SampleIndex;
                SoundInfo[i * 24 + 6] = So.Flags;
            }
            writer.Write(new ZsndHeader(InfoSize, SoundCount, SampleCount)
            {
                Platform = Root.Platform.Magic,
                Size = Size,
                SoundHashesOffset = HeaderSize,
                SoundsOffset = soundsOffset,
                SampleHashesOffset = sampleHOffset,
                SamplesOffset = samplesOffset,
                SampleFileHashesOffset = sampleFHOffset,
                SampleFilesOffset = sampleFOffset
            });
            // System.Diagnostics.Debug.Assert(fs.Position == HeaderSize);
            writer.Write(SoundHashes);
            // System.Diagnostics.Debug.Assert(fs.Position == soundsOffset);
            writer.Write(SoundInfo);
            // System.Diagnostics.Debug.Assert(fs.Position == sampleHOffset);
            writer.Write(FileHashesBase, "CHARS3/7R");
            // System.Diagnostics.Debug.Assert(fs.Position == samplesOffset);
            writer.Write(SampleInfo32);
            // System.Diagnostics.Debug.Assert(fs.Position == sampleFHOffset);
            writer.Write(FileHashesBase, "FILE");
            // System.Diagnostics.Debug.Assert(fs.Position == sampleFOffset);
            writer.Write(FileInfosB);
            // System.Diagnostics.Debug.Assert(fs.Position == InfoSize);
        }
        /// <summary>
        /// Round a <paramref name="Rate"/> value to a base of 10 (5 rounds down), if it's not (already) a whole number.
        /// </summary>
        private static uint RoundPS2Rate(double Rate) { return (uint)(Rate % 1 == 0 ? Rate : Math.Round(Rate / 10.0) * 10); }
        /// <summary>
        /// Reads a span of 4-byte unsigned integers from a span of <paramref name="bytes"/>. (Depending on <paramref name="NeedReverse"/>.)
        /// </summary>
        /// <returns>A <see cref="uint"/> span, as a reversed copy if it needs to be reversed; otherwise as a view of the original span.</returns>
        private static Span<uint> ToUint32Span(Span<byte> bytes, bool NeedReverse)
        {
            if (NeedReverse)
            {
                Span<uint> result = new uint[bytes.Length / 4]; bytes.CopyTo(MemoryMarshal.AsBytes(result));
                BinaryPrimitives.ReverseEndianness(result, result);
                return result;
            }
            return MemoryMarshal.Cast<byte, uint>(bytes);
        }
        /// <summary>
        /// Reads a span of 4-byte unsigned integers from a span of <paramref name="bytes"/>. (Depending on <paramref name="NeedReverse"/>.)
        /// </summary>
        /// <returns>A <see cref="ushort"/> span, as a reversed copy if it needs to be reversed; otherwise as a view of the original span.</returns>
        private static Span<ushort> ToUint16Span(Span<byte> bytes, bool NeedReverse)
        {
            if (NeedReverse)
            {
                Span<ushort> result = new ushort[bytes.Length / 2]; bytes.CopyTo(MemoryMarshal.AsBytes(result));
                BinaryPrimitives.ReverseEndianness(result, result);
                return result;
            }
            return MemoryMarshal.Cast<byte, ushort>(bytes);
        }
    }
}
