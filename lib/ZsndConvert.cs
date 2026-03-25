using System;
using System.Buffers;
using System.Buffers.Binary;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Zsnd_UI.Functions;

// IMPORTANT NOTE:
// RIFF/WAVE FORMAT IS DEFINED AS (PARTLY) MACHINE INDEPENDENT LITTLE ENDIAN. NOT SURE IF THIS IS CORRECT.
namespace Zsnd_UI.lib
{
    /// <summary>
    /// RIFF header as used by Microsoft digital sound files (WAVE, XNA formats, etc.), focused on the WAVE format.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct PCM_RIFF_Header
    {
        public uint Riff;
        public uint FileSize;
        public uint RiffType;
        public uint Fmt;
        public uint FmtSize;
        public ushort AudioFormat;
        public ushort Channels;
        public uint SampleRate;
        public uint ByteRate;
        public ushort BlockAlign;
        public ushort BitsPerSample;
        // other data depending on format
        public uint Data;
        public uint DataSize;

        private readonly bool DataCheck => Data == SoundIDs.DATA;
        public readonly bool FmtCheck => Riff == SoundIDs.RIFF && RiffType == SoundIDs.WAVE && Fmt == SoundIDs.FMT;
        /// <summary>
        /// Initialize a new <see cref="PCM_RIFF_Header"/> instance with standard values for a PCM 16 bit WAVE file.
        /// </summary>
        public PCM_RIFF_Header()
        {
            Riff = SoundIDs.RIFF;
            RiffType = SoundIDs.WAVE;
            Fmt = SoundIDs.FMT;
            FmtSize = 16; // PCM
            AudioFormat = 1;
            BitsPerSample = 16;
            Data = SoundIDs.DATA;
        }
        /// <summary>
        /// Parse data from a <paramref name="stream"/> as <see cref="PCM_RIFF_Header"/>.
        /// </summary>
        /// <remarks>Advances the stream by the size of <see cref="PCM_RIFF_Header"/>.</remarks>
        /// <returns>The data from the <paramref name="stream"/> as <see cref="PCM_RIFF_Header"/>, if the <paramref name="stream"/> provides sufficient data, otherwise <see langword="null"/>.</returns>
        public static PCM_RIFF_Header? FromStream(FileStream stream)
        {
            int headerSize = Marshal.SizeOf<PCM_RIFF_Header>();
            Span<byte> headerBytes = stackalloc byte[headerSize];
            return stream.Read(headerBytes) == headerSize ? MemoryMarshal.Read<PCM_RIFF_Header>(headerBytes) : null;
        }
        /// <summary>
        /// Write the data from this <see cref="PCM_RIFF_Header"/> instance to a <paramref name="stream"/>.
        /// </summary>
        /// <remarks>Advances the stream by the size of <see cref="PCM_RIFF_Header"/>.</remarks>
        //public readonly void ToStream(FileStream stream)
        //{
        //    ReadOnlySpan<byte> headerBytes = MemoryMarshal.AsBytes(MemoryMarshal.
        //        CreateReadOnlySpan(ref Unsafe.AsRef(in this), 1));
        //    stream.Write(headerBytes);
        //
        //}
        /// <summary>
        /// Check if the <see cref="PCM_RIFF_Header"/> values match a valid 16bit header, including data sizes, according to the <paramref name="stream"/>.
        /// </summary>
        /// <returns> Whether the data looks valid.</returns>
        public readonly bool IsValid(FileStream stream)
        {
            return AudioFormat == 1 && Channels > 0 && SampleRate > 0 // little endian check
                   && BitsPerSample == 16 // compatibility check
                   && DataCheck && FileSize < stream.Length // valid stream check
                   && DataSize > 1 && stream.Position + DataSize <= FileSize + 8; // valid data check
        }
    }
    /// <summary>
    /// XMA per sample sub-header for the XMA RIFF header as used by Microsoft's .xma sound format for consoles.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct XMA_Stream_Header
    {
        public uint Unknown; // Related to size
        public uint SampleRate;
        public uint LoopStart;
        public uint LoopEnd;
        public byte LoopSubFrame;
        public byte Channels;
        public ushort UnknownFlag;
    }
    /// <summary>
    /// XMA RIFF header as used by Microsoft's .xma sound format for consoles. Similar to PCM RIFF, but significant changes.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct XMA_RIFF_Header
    {
        public uint Riff;
        public uint FileSize;
        public uint RiffType;
        public uint Fmt;
        public uint FmtSize;
        public ushort AudioFormat;
        public ushort Unk2; // Channels unused?
        public uint Unk4;   // SampleRate unused?
        public ushort NumStreams; //
        public byte Loop;         // ByteRate equivalent?
        public byte LoopPadding;  //

        public ushort Channels { get; private set; }
        public uint SampleRate { get; private set; }

        private readonly bool FmtCheck => Riff == SoundIDs.RIFF && RiffType == SoundIDs.WAVE && Fmt == SoundIDs.FMT; // RIFF WAVEfmt check

        /// <summary>
        /// Parse data from a <paramref name="stream"/> as <see cref="XMA_RIFF_Header"/>.
        /// </summary>
        /// <remarks>Advances the stream by the size of <see cref="XMA_RIFF_Header"/>.</remarks>
        /// <returns>The data from the <paramref name="stream"/> as <see cref="XMA_RIFF_Header"/>, if the <paramref name="stream"/> provides valid data, otherwise <see langword="null"/>.</returns>
        public static XMA_RIFF_Header? FromStream(FileStream stream)
        {
            int headerSize = Marshal.SizeOf<XMA_RIFF_Header>();
            Span<byte> headerBytes = stackalloc byte[headerSize];
            if (stream.Read(headerBytes) == headerSize)
            {
                XMA_RIFF_Header header = MemoryMarshal.Read<XMA_RIFF_Header>(headerBytes);
                if (header.FmtCheck)
                {
                    for (int i = 0; i < header.NumStreams; i++)
                    {
                        int SHSZ = Marshal.SizeOf<XMA_Stream_Header>(); headerBytes = headerBytes[..SHSZ];
                        if (stream.Read(headerBytes) != SHSZ) { return null; }
                        XMA_Stream_Header SH = MemoryMarshal.Read<XMA_Stream_Header>(headerBytes);
                        header.Channels += SH.Channels;
                        header.SampleRate = SH.SampleRate;
                    }
                    return header.AudioFormat == 0x0165 && header.Channels > 0 && header.SampleRate > 0 // little endian check
                        && header.FileSize < stream.Length // valid stream check
                        ? header : null;
                }
            }
            return null;
        }
    }
    /// <summary>
    /// Essential header info as used by Sony Playstation's .vag sound format for the PS2, PS3 and PSP consoles.
    /// </summary>
    public struct VagHeader(uint Size = 0, uint Rate = 0, string Name = "")
    {
        public uint Size = Size;
        public uint SampleRate = Rate;
        public string FileName = Name;

        /// <summary>
        /// Read essential data from a <paramref name="stream"/> and save it to this <see cref="VagHeader"/>.
        /// </summary>
        /// <remarks>Advances the stream by 48 (complete header size). Forces big endian, because PS systems are BE.</remarks>
        /// <returns><see langword="True"/>, if the <paramref name="stream"/> length is sufficient for the header and parsed data size; otherwise <see langword="false"/>.</returns>
        public bool FromStream(FileStream stream)
        {
            Span<byte> buffer = stackalloc byte[0x30];
            if (stream.Read(buffer) == 0x30 && BinaryPrimitives.ReadUInt32LittleEndian(buffer) == SoundIDs.VAG) // ID is endian independent
            {
                Size = BinaryPrimitives.ReadUInt32BigEndian(buffer.Slice(12, 4));
                SampleRate = BinaryPrimitives.ReadUInt32BigEndian(buffer.Slice(16, 4));
                // don't need file name for Zsnd purposes
                return stream.Length - 0x30 >= Size;
            }
            return false;
        }
        /// <summary>
        /// Write the data from this <see cref="VagHeader"/> instance to a <paramref name="stream"/>, filling the missing info with default values.
        /// </summary>
        /// <remarks>Advances the stream by 48 (complete header size).</remarks>
        public readonly ReadOnlyMemory<byte> ToMemory()
        {
            byte[] bytes = new byte[0x30]; Span<byte> buffer = bytes;
            // alt.: Encoding.ASCII.GetBytes("VAGp", 0, 4, buffer, 0);
            BinaryPrimitives.WriteUInt32BigEndian(buffer[..4], SoundIDs.VAG);
            buffer[7] = 0x20;
            // offsets 8, and 0x14, 0x18, 0x1C are unused (uint 0)
            BinaryPrimitives.WriteUInt32BigEndian(buffer.Slice(0x0C, 4), Size);
            BinaryPrimitives.WriteUInt32BigEndian(buffer.Slice(0x10, 4), SampleRate);
            _ = System.Text.Encoding.UTF8.GetBytes(FileName.Length > 0x10 ? FileName.AsSpan(0, 0x10) : FileName, buffer[0x20..]);
            return bytes;
        }
    }
    /// <summary>
    /// Minimalistic class with sound ID constants
    /// </summary>
    public static class SoundIDs
    {
        // Little Endian values (Currently assuming Wave are always LE)
        public const uint WAVE = 0x45564157; // "WAVE" little endian
        public const uint FMT = 0x20746d66;  // "fmt " little endian
        public const uint DATA = 0x61746164; // "data" little endian
        // Big Endian values, depending on build (machine endianness)
#if BIGENDIAN
        public const uint ZSND = 0x5A534E44;
        public const uint DSP = 0x44535020;
        public const uint RIFF = 0x52494646;
        public const uint VAG = 0x56414770;  // "VAGp"
#else
        public const uint ZSND = 0x444E535A;
        public const uint DSP = 0x20505344;
        public const uint RIFF = 0x46464952;
        public const uint VAG = 0x70474156;  // "VAGp"
#endif
        /// <summary>
        /// Reads the first four bytes from the specified file <paramref name="stream"/> as an unsigned 32-bit integer (big-endian).
        /// </summary>
        /// <remarks>The position is advanced by 4 from the current position (should be 0, so the new position should be 4).</remarks>
        /// <returns>An <see cref="uint"/> representing the big-endian value of 4 bytes of the <paramref name="stream"/> at its position, if it can be read for 4 bytes; otherwise 0.</returns>
        //private static uint GetMagic(FileStream stream)
        //{
        //    try
        //    {   // span.SequenceEqual() is slower; BitConverter.ToUInt32(span) depends on machine endianness
        //        Span<byte> span = stackalloc byte[4]; // new and dispose instead of using reserved memory
        //        stream.ReadExactly(span);
        //        return BinaryPrimitives.ReadUInt32BigEndian(span);
        //    }   // BinaryPrimitives.ReadUInt32LittleEndian(span) is only very slightly faster on LE machines
        //    catch { return 0u; }
        //}
        /// <summary>
        /// Reads the "magic" ID from a <paramref name="file"/> as <see cref="uint"/> and checks if it's a defined ID.
        /// </summary>
        /// <returns><see langword="True"/>, if the read ID is "RIFF" or "VAGp"; otherwise, <see langword="false"/>.</returns>
        public static bool InMagicOf(string file)
        {
            try
            {
                Span<byte> buffer = stackalloc byte[4];
                using FileStream fs = new(file, FileMode.Open, FileAccess.Read, FileShare.Read);
                fs.ReadExactly(buffer);
                return MemoryMarshal.Cast<byte, uint>(buffer)[0] is RIFF or VAG;
            }
            catch { return false; }
        }
    }
    /// <summary>
    /// Conversion class from and to .zss/.zsm data.
    /// </summary>
    internal static class ZsndConvert
    {
        /// <summary>
        /// Convert a Zsnd sound file (.zss/.zsm entry) to a standard .wav file, according to the <paramref name="SampleInfo"/>.
        /// </summary>
        /// <returns>The path from <paramref name="SampleInfo"/> if it's already a playable file, otherwise the path to a newly converted playable file in <paramref name="ExtractDir"/>.</returns>
        public static async Task<string?> To(UISample SampleInfo, string ExtractDir, CancellationToken Token = default)
        {
            string TargetPath = Path.Combine(ExtractDir, SampleInfo.Name!);
            string Ext = Path.GetExtension(SampleInfo.Name!);
            bool exists = File.Exists(TargetPath);
            if (!(exists
                && (Ext.Equals(".dsp", StringComparison.OrdinalIgnoreCase)
                || Ext.Equals(".xma", StringComparison.OrdinalIgnoreCase)
                || SoundIDs.InMagicOf(TargetPath))))
            {
                // RIFF or VAGp files without headers, or not exists
                string SourcePath;
                if (exists)
                {
                    SourcePath = SampleInfo.File = ZsndPath.GetHeaderlessPath(TargetPath);
                    File.Move(TargetPath, SourcePath, true);
                }
                else if (SampleInfo.File is null || !(SampleInfo.File[1] is ':' or '/' or '\\'))
                {
                    // Throws exceptions if file isn't found or accessible, which we catch at callers
                    SourcePath = ZsndPath.GetHeaderlessPath(TargetPath);
                }
                else
                {
                    SourcePath = SampleInfo.File;
                    _ = ZsndPath.CreateDirectory(TargetPath);
                }
                // We could make a raw copy here for raw support, but with a header file, the rate should already be accurate
                if (!exists && SoundIDs.InMagicOf(SourcePath)) { File.Copy(SourcePath, TargetPath); }
                else
                {
                    using Microsoft.Win32.SafeHandles.SafeFileHandle fh = File.OpenHandle(
                        SourcePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                    await To(SampleInfo, fh, Ext, TargetPath, Token);
                }
            }
            return Ext.Equals(".wav", StringComparison.OrdinalIgnoreCase)
                || Ext.Equals(".xbadpcm", StringComparison.OrdinalIgnoreCase)
                ? TargetPath
                : Util.RunExeInCmd(Path.Combine(ZsndPath.CD, "vgmstream", "vgmstream-cli.exe"), TargetPath)
                ? $"{TargetPath}.wav"
                : null;
            // VGMstream Notes: Outputs to the same path with .wav extension, we might want to remove that temporary file (WIP?)
            //                  Supports returning data to stdout with the -p flag, but format is unknown, and we prefer saved files
            //                  Doesn't seem to support other formats than .wav (shouldn't work: $"{TargetPath} -o ?f{Format}", where Format is argument)
        }
        /// <summary>
        /// Convert a Zsnd sound file (.zss/.zsm entry) to a standard file at <paramref name="TargetPath"/>, according to the <paramref name="SampleInfo"/> and <paramref name="Ext"/>ension.
        /// </summary>
        /// <param name="SourceFile">The Zsnd file (.zss/.zsm or raw audio file)</param>
        /// <param name="SampleInfo">The Zsnd sound file info (.zss/.zsm entry)</param>
        public static async Task To(UISample SampleInfo, Microsoft.Win32.SafeHandles.SafeFileHandle SourceFile,
            string Ext, string TargetPath, CancellationToken Token)
        {
            // WIP: What to do, if it already exists?
            if (Ext.Equals(".wav", StringComparison.OrdinalIgnoreCase))
            { await ToWav(SourceFile, SampleInfo, TargetPath, Token); }
            else if (Ext.Equals(".xbadpcm", StringComparison.OrdinalIgnoreCase))
            { await ToWav(SourceFile, SampleInfo, TargetPath, Token, 0x20); } // Path.ChangeExtension(TargetPath, ".wav")
            else if (Ext.Equals(".vag", StringComparison.OrdinalIgnoreCase))
            { await VagAddHeader(SourceFile, SampleInfo.SampleRate, TargetPath, Token); }
            else if (SampleInfo.Size != 0 && (Ext.Equals(".dsp", StringComparison.OrdinalIgnoreCase)
                                          || Ext.Equals(".xma", StringComparison.OrdinalIgnoreCase)))
            {
                using FileStream fs = new(TargetPath, FileMode.Create, FileAccess.Write, FileShare.None, 0x1000, FileOptions.Asynchronous);
                await Extensions.CopyToAsync(SourceFile, fs, SampleInfo.Offset, (int)SampleInfo.Size, Token);
            }
            // Otherwise, playable (check at caller)
        }
        /// <summary>
        /// Convert a standard sound file to a file to be used in Zsnd files (.zss/.zsm), according to the lowercase <paramref name="Ext"/>ension, and write the info to <paramref name="SampleInfo"/>.
        /// </summary>
        /// <param name="SamplePath">The standard sound file</param>
        /// <returns>A <see cref="byte"/> array with the converted data (empty if conversion fails).</returns>
        public static Span<byte> From(string Ext, string SamplePath, UISample SampleInfo, ZsndPlatform Plat)
        {
            try
            {
                // WAV and VAG are headerless, DSP has special header, XMA has original RIFF header, Xbox ADPCM seems to contain converted data, just stripping header ATM
                return Ext.Equals(".wav", StringComparison.OrdinalIgnoreCase) && Plat.ID is ZsndProperties.Platform.PC or ZsndProperties.Platform.XBOX
                       ? XNA_ADPCM.Encode(SamplePath, SampleInfo, Plat.ID is ZsndProperties.Platform.PC ? 0 : 0x20)
                       : Ext.Equals(".xbadpcm", StringComparison.OrdinalIgnoreCase) && Plat.ID is ZsndProperties.Platform.XBOX
                       ? XboxHeaderStrip(SamplePath, SampleInfo)
                       : Ext.Equals(".vag", StringComparison.OrdinalIgnoreCase) && Plat.IsPS
                       ? VagHeaderStrip(SamplePath, SampleInfo)
                       : Ext.Equals(".dsp", StringComparison.OrdinalIgnoreCase) && Plat.ID is ZsndProperties.Platform.GCUB
                       ? DspReadInfo(SamplePath, SampleInfo)
                       : Ext.Equals(".xma", StringComparison.OrdinalIgnoreCase) && Plat.ID is ZsndProperties.Platform.XENO
                       ? XmaReadInfo(SamplePath, SampleInfo)
                       : [];
                // Note: vgmstream can ONLY convert TO WAV. Might add other formats in the future
            }
            catch { return []; }
        }

        private static async Task ToWav(Microsoft.Win32.SafeHandles.SafeFileHandle SourceFile,
            UISample SampleInfo, string TargetPath, CancellationToken Token, int BlockSize = 0)
        {
            if (SampleInfo.Size == 0) { SampleInfo.Size = (uint)RandomAccess.GetLength(SourceFile); }
            int Size = (int)SampleInfo.Size;
            byte[] buffer = ArrayPool<byte>.Shared.Rent(Size);
            Memory<byte> mbuffer = buffer.AsMemory(0, Size);
            try
            {
                await Extensions.ReadExactlyAsync(SourceFile, mbuffer, SampleInfo.Offset, Size, Token);
                await XNA_ADPCM.DecodeAsync(mbuffer, TargetPath, SampleInfo, BlockSize, Token).ConfigureAwait(false);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        private static async Task VagAddHeader(Microsoft.Win32.SafeHandles.SafeFileHandle SourceFile,
            uint SampleRate, string TargetPath, CancellationToken Token)
        {
            using FileStream fs = new(SourceFile, FileAccess.Read, 0x1000, true);
            using FileStream tfs = new(TargetPath, FileMode.Create, FileAccess.Write, FileShare.None, 0x1000, FileOptions.Asynchronous);
            VagHeader Header = new((uint)fs.Length, SampleRate, Path.GetFileName(TargetPath));  // only 1 channel
            await tfs.WriteAsync(Header.ToMemory(), Token);
            await fs.CopyToAsync(tfs, Token).ConfigureAwait(false);
        }

        private static Span<byte> VagHeaderStrip(string SamplePath, UISample SampleInfo)
        {
            VagHeader Header = new();
            using FileStream fs = new(SamplePath, FileMode.Open, FileAccess.Read);
            if (Header.FromStream(fs))
            {
                Span<byte> Samples = new byte[Header.Size];
                fs.ReadExactly(Samples);
                // Note: VAG are always mono, as the official format doesn't support stereo
                SampleInfo.SampleRate = Header.SampleRate;
                return Samples;
            }
            return [];
        }

        private static Span<byte> XboxHeaderStrip(string SamplePath, UISample SampleInfo)
        {
            // Universal WAV header strip, but with AudioFormat == 0x69 check
            using FileStream fs = new(SamplePath, FileMode.Open, FileAccess.Read);
            PCM_WAVE_Reader WAV = new(fs);
            if (WAV.Header.AudioFormat == 0x69)
            {
                Span<byte> Samples = new byte[WAV.Header.DataSize];
                fs.ReadExactly(Samples);
                SampleInfo.SampleRate = WAV.Header.SampleRate;
                if (WAV.Header.Channels > 1) { SampleInfo.Flags |= WAV.Header.Channels > 3 ? ZsndProperties.SampleF.FourChannels : ZsndProperties.SampleF.Stereo; }
                return Samples;
            }
            return [];
        }

        private static Span<byte> DspReadInfo(string SamplePath, UISample SampleInfo)
        {
            using FileStream fs = new(SamplePath, FileMode.Open, FileAccess.Read);
            Span<byte> buffer = stackalloc byte[4];
            fs.ReadExactly(buffer); fs.Position = 8;
            // Sample count should match the channel data length, but this is just a quick way to guess the channels
            float Channels = (float)fs.Length / BinaryPrimitives.ReadUInt32BigEndian(buffer);
            fs.ReadExactly(buffer);
            SampleInfo.SampleRate = BinaryPrimitives.ReadUInt32BigEndian(buffer);
            fs.ReadExactly(buffer);
            // Unknown if the Zsnd format supports more than 2 channel DSP or if the flags are accurate
            if (Channels > 1) { SampleInfo.Flags |= Channels > 3 ? ZsndProperties.SampleF.FourChannels : ZsndProperties.SampleF.Stereo; }
            if (buffer[1] == 1) { SampleInfo.Flags |= ZsndProperties.SampleF.Loop; } // otherwise should be 0
            return [];
        }

        private static Span<byte> XmaReadInfo(string SamplePath, UISample SampleInfo)
        {
            // https://stackoverflow.com/questions/70992562/c-xbox360-application-xaudio2-playing-a-xma-sound
            using FileStream fs = new(SamplePath, FileMode.Open, FileAccess.Read);
            if (XMA_RIFF_Header.FromStream(fs) is XMA_RIFF_Header header)
            {
                SampleInfo.SampleRate = header.SampleRate;
                if (header.Channels > 1) { SampleInfo.Flags |= header.Channels > 3 ? ZsndProperties.SampleF.FourChannels : ZsndProperties.SampleF.Stereo; }
                if (header.Loop > 0) { SampleInfo.Flags |= ZsndProperties.SampleF.Loop; }
            }
            return [];
        }
    }

    internal class PCM_WAVE_Reader
    {
        private readonly FileStream _stream;
        private readonly byte[] _buffer = new byte[8];

        public readonly PCM_RIFF_Header Header;
        public readonly bool IsValid;

        public PCM_WAVE_Reader(FileStream fs)
        {
            _stream = fs;

            if (PCM_RIFF_Header.FromStream(fs) is PCM_RIFF_Header header && header.FmtCheck)
            {
                // Handle special formats and header info, but only if the first chunk is fmt (is this guaranteed?)
                fs.Position = 20 + header.FmtSize;
                while (fs.Read(_buffer, 0, 8) == 8)
                {
                    uint chunkId = BinaryPrimitives.ReadUInt32LittleEndian(_buffer.AsSpan(0, 4));
                    uint chunkSize = BinaryPrimitives.ReadUInt32LittleEndian(_buffer.AsSpan(4, 4));
                    if (fs.Position + chunkSize > header.FileSize + 8)
                    {
                        break;
                    }
                    else if (chunkId == SoundIDs.DATA)
                    {
                        header.DataSize = chunkSize;
                        break; // Stop reading after the 'data' chunk, so the _stream pos is at the data
                    }
                    _ = fs.Seek(chunkSize, SeekOrigin.Current);
                }
                IsValid = header.IsValid(fs);
                Header = header;
            }
        }
        /// <summary>
        /// Calls <see cref="BinaryPrimitives.ReadUInt16LittleEndian"/> using the <see cref="_stream"/> at the current position (doesn't handle end of data!).
        /// </summary>
        /// <returns>The <see cref="short"/> as returned by <see cref="BinaryPrimitives.ReadUInt16LittleEndian"/>.</returns>
        public short ReadSample()
        {
            _stream.ReadExactly(_buffer.AsSpan(0, 2));
            return BinaryPrimitives.ReadInt16LittleEndian(_buffer);
        }
    }

    internal struct IMAState
    {
        public int PredictedSample; // short, but int performs better
        public int StepIndex;
    }
    /// <summary>
    /// Static methods for encoding and decoding audio data using the XNA ADPCM (Adaptive Differential Pulse Code Modulation) format.
    /// </summary>
    /// <remarks>Based on RavenAudio <see href="https://github.com/nikita488/ravenAudio/blob/master/src/main.cpp"/>. Thread safety is not guaranteed.</remarks>
    internal static class XNA_ADPCM
    {
        internal const byte MaxStepIndex = 88;
        private static readonly short[] StepSizes =
        [
        7, 8, 9, 10, 11, 12, 13, 14,
        16, 17, 19, 21, 23, 25, 28, 31,
        34, 37, 41, 45, 50, 55, 60, 66,
        73, 80, 88, 97, 107, 118, 130, 143,
        157, 173, 190, 209, 230, 253, 279, 307,
        337, 371, 408, 449, 494, 544, 598, 658,
        724, 796, 876, 963, 1060, 1166, 1282, 1411,
        1552, 1707, 1878, 2066, 2272, 2499, 2749, 3024,
        3327, 3660, 4026, 4428, 4871, 5358, 5894, 6484,
        7132, 7845, 8630, 9493, 10442, 11487, 12635, 13899,
        15289, 16818, 18500, 20350, 22385, 24623, 27086, 29794,
        32767
        ];

        // https://github.com/Sergeanur/XboxADPCM/blob/767dc2640f8de4ac1f4fd6badfd13a402b3d1713/XboxADPCM/ImaADPCM.cpp#L67
        private static readonly sbyte[] StepIndices = [-1, -1, -1, -1, 2, 4, 6, 8];

        private const int MusicBlockSize = 0x4000;
        private const int HalfBlockSize = 0x2000; // MusicBlockSize / 2
        /// <summary>
        /// Decode a file in XNA ADPCM format from <paramref name="EncodedBytes"/> and write it as PCM RIFF WAVE format to <paramref name="TargetPath"/>, using the specified <paramref name="Channels"/>, <paramref name="SampleRate"/> and <paramref name="BlockSize"/> (if applicable).
        /// </summary>
        /// <remarks>Exceptions: System.IO; NotSupportedException (channels).</remarks>
        public static async Task DecodeAsync(ReadOnlyMemory<byte> EncodedBytes, string TargetPath, UISample SampleInfo, int BlockSize, CancellationToken Token)
        {
            byte[] DecodedBytes = ArrayPool<byte>.Shared.Rent(EncodedBytes.Length * 4);
            ushort Channels = (ushort)(SampleInfo.Flags.HasFlag(ZsndProperties.SampleF.Stereo)
                ? SampleInfo.Flags.HasFlag(ZsndProperties.SampleF.AmbientEmbedded) ? 4 : 2 : 1);
            try
            {
                uint DecodedLength = await Task.Run(() =>
                {
                    return Decode(EncodedBytes.Span, DecodedBytes, Channels, BlockSize);
                }, Token).ConfigureAwait(false);

                byte[] HeaderBytes = new byte[44]; // Marshal.SizeOf<PCM_RIFF_Header>()
                Span<PCM_RIFF_Header> Header = MemoryMarshal.Cast<byte, PCM_RIFF_Header>(HeaderBytes.AsSpan());
                Header[0] = new()
                {
                    Channels = Channels,
                    SampleRate = SampleInfo.SampleRate,
                    ByteRate = Channels * SampleInfo.SampleRate * 2, // 2: BitsPerSample (16) / 8
                    BlockAlign = (ushort)(Channels * 2),
                    DataSize = DecodedLength,
                    FileSize = 36 + DecodedLength // HeaderBytes.Length - 8
                };
                using FileStream Out = new(TargetPath, FileMode.Create, FileAccess.Write, FileShare.None, 0x1000, FileOptions.Asynchronous);
                await Out.WriteAsync(HeaderBytes, Token).ConfigureAwait(false);
                await Out.WriteAsync(DecodedBytes.AsMemory(0, (int)DecodedLength), Token).ConfigureAwait(false);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(DecodedBytes);
            }
        }
        /// <summary>
        /// Decode a file in XNA ADPCM format from <paramref name="EncodedBytes"/> to <paramref name="DecodedBytes"/>, using the specified <paramref name="Channels"/> and <paramref name="BlockSize"/> (if applicable).
        /// </summary>
        /// <returns>The number of bytes written to <paramref name="DecodedBytes"/> (samples written * 2).</returns>
        /// <remarks>Input and output data are limited to 2GB, because of the use of span and int.</remarks>
        /// <exception cref="NotSupportedException">If the number of <paramref name="Channels"/> is not supported.</exception>
        public static uint Decode(ReadOnlySpan<byte> EncodedBytes, Span<byte> DecodedBytes, ushort Channels, int BlockSize)
        {
            // We might have to assert that Size is less than 1GB for the sake of output (2GB limit)
            int Size = EncodedBytes.Length;
            Span<short> DecodedSamples = MemoryMarshal.Cast<byte, short>(DecodedBytes);
            int x = 0;
            if (BlockSize != 0)
            {
                // Microsoft XNA Game Studio [XNB]: First 4 bytes of a block contain initialization information.
                // https://stackoverflow.com/questions/9541471/problems-converting-adpcm-to-pcm-in-xna
                Span<IMAState> States = stackalloc IMAState[Channels];
                ReadOnlySpan<short> EncodedSamples = MemoryMarshal.Cast<byte, short>(EncodedBytes);
                //#if BIGENDIAN
                //BinaryPrimitives.ReverseEndianness(EncodedSamples, EncodedSamples); // not needed? the original code worked on LE, BE is unknown
                //#endif
                BlockSize += 4;
                int SizePerCh = Size / Channels;
                for (int i = 0; i < SizePerCh; i += BlockSize)
                {
                    for (ushort ch = 0; ch < Channels; ch++)
                    {
                        ref IMAState State = ref States[ch];
                        State.PredictedSample = DecodedSamples[x++] = EncodedSamples[i / 2];
                        State.StepIndex = EncodedBytes[i + 2]; // fourth byte unused? depends on endian?
                    }
                    for (int j = (i + 4) * Channels, EndBlock = (i + BlockSize) * Channels; j < EndBlock;)
                    {
                        for (ushort ch = 0; ch < Channels; ch++, j++)
                        {
                            ref IMAState State = ref States[ch];
                            Decode(EncodedBytes[j], ref State, ref State, DecodedSamples, ref x);
                        }
                    }
                }
            }
            else if (Channels == 1)
            {
                IMAState State = new();
                for (int i = 0; i < Size; i++)
                {
                    Decode(EncodedBytes[i], ref State, ref State, DecodedSamples, ref x);
                }
            }
            else if (Channels == 2)
            {
                IMAState Left = new(), Right = new();
                for (int i = 0; i < Size; i++)
                {
                    Decode(EncodedBytes[i], ref Left, ref Right, DecodedSamples, ref x);
                }
            }
            else if (Channels == 4)
            {
                IMAState Ch0 = new(), Ch1 = new(), Ch2 = new(), Ch3 = new();
                for (int BI = 0; BI < Size; BI += MusicBlockSize)
                {
                    int LastIPerTrack = BI + Math.Min(MusicBlockSize, Size - BI) - HalfBlockSize;
                    for (int i = BI; i < LastIPerTrack; i++)
                    {
                        Decode(EncodedBytes[i], ref Ch0, ref Ch1, DecodedSamples, ref x);
                        Decode(EncodedBytes[i + HalfBlockSize], ref Ch2, ref Ch3, DecodedSamples, ref x);
                    }
                }
                // EncodedBytes.Length has full block for first channel-pair remainder
                // return (Size - MusicBlockSize + (Size % MusicBlockSize)) / 2
            }
            else
            {
                throw new NotSupportedException($"Decoding for {Channels} channels is not supported.");
            }
            return (uint)(x * 2);
        }
        /// <summary>
        /// Store two 16 bit PCM sample shorts (two bytes), decoded from <paramref name="EncodedByte"/> into <paramref name="DecodedSamples"/> at inde<paramref name="x"/>.
        /// </summary>
        /// <param name="LowCh">Holds the first short (low/left channel)</param>
        /// <param name="HighCh">Holds the second short (high/right channel)</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Decode(byte EncodedByte, ref IMAState LowCh, ref IMAState HighCh, Span<short> DecodedSamples, ref int x)
        {
            Decode((byte)(EncodedByte & 0xF), ref LowCh);
            DecodedSamples[x++] = (short)LowCh.PredictedSample; // again, LE confirmed, BE unconfirmed
            Decode((byte)(EncodedByte >> 4), ref HighCh);
            DecodedSamples[x++] = (short)HighCh.PredictedSample;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Decode(byte EncodedSample, ref IMAState State)
        {
            // originalsample + 0.5 * stepSize / 4 + stepSize / 8 optimization. (the below calculation is equivalent, but less truncation)
            //http://www.cs.columbia.edu/~hgs/audio/dvi/p34.jpg
            //https://github.com/Sergeanur/XboxADPCM/blob/767dc2640f8de4ac1f4fd6badfd13a402b3d1713/XboxADPCM/ImaADPCM.cpp#L20
            int StepSize = StepSizes[State.StepIndex], diff = ((EncodedSample & 7) * StepSize >> 2) + (StepSize >> 3);
            // seemingly from relative to absolute with signed diff
            State.PredictedSample = Math.Clamp(State.PredictedSample + ((EncodedSample & 8) == 0 ? diff : -diff), short.MinValue, short.MaxValue);
            // update from half sized StepIndices
            State.StepIndex = Math.Clamp(State.StepIndex + StepIndices[EncodedSample & 7], byte.MinValue, MaxStepIndex);
            // Original code, more truncation
            //int diff = (StepSize >> 3) +
            //           (StepSize * ((EncodedSample >> 2) & 1)) +
            //           ((StepSize >> 1) * ((EncodedSample >> 1) & 1)) +
            //           ((StepSize >> 2) * (EncodedSample & 1));
        }

        public static Span<byte> Encode(string SamplePath, UISample SampleInfo, int BlockSize = 0)
        {
            using FileStream fs = new(SamplePath, FileMode.Open, FileAccess.Read);
            PCM_WAVE_Reader WAV = new(fs);
            if (!WAV.IsValid) { return []; }
            // Export info
            SampleInfo.SampleRate = WAV.Header.SampleRate;
            if (WAV.Header.Channels > 1) { SampleInfo.Flags |= WAV.Header.Channels == 4 ? ZsndProperties.SampleF.FourChannels : ZsndProperties.SampleF.Stereo; }
            else if (BlockSize == 0) { SampleInfo.Format = 106; } // WIP: Other formats?

            int EncodedSize = (int)(WAV.Header.DataSize / 4); // max. 1GB
            if (BlockSize > 0)
            {
                int WavBSz = (BlockSize * 4) + 2;
                long ExtraSz = WAV.Header.DataSize % (WavBSz * WAV.Header.Channels);
                EncodedSize = (int)((WAV.Header.DataSize - ExtraSz) * (BlockSize + 4) / WavBSz);
                if (ExtraSz > 0) { EncodedSize += (BlockSize + 4) * WAV.Header.Channels; }
            }
            else if (WAV.Header.Channels == 4)
            {
                EncodedSize += (MusicBlockSize - (EncodedSize % MusicBlockSize)) / 2;
            }
            //byte[] EncodedBytes = new byte[EncodedSize];
            // For the sake of DecodedBytes, EncodedSize should be checked to be max 500MB (500MB * 4 <= 2GB)
            Span<byte> EncodedBytes = new byte[EncodedSize];
            Span<byte> DecodedBytes = new byte[WAV.Header.DataSize];
            Span<short> DecodedSamples = MemoryMarshal.Cast<byte, short>(DecodedBytes);
            fs.ReadExactly(DecodedBytes);

            if (BlockSize > 0)
            {
                bool IsMono = WAV.Header.Channels == 1;
                Span<IMAState> States = stackalloc IMAState[WAV.Header.Channels];
                Span<short> EncodedInitSamples = MemoryMarshal.Cast<byte, short>(EncodedBytes);
                long MaxDecodedIx = WAV.Header.DataSize / 2;
                for (int i = 0, x = 0; i < EncodedSize && x < MaxDecodedIx;)
                {
                    for (ushort ch = 0; ch < WAV.Header.Channels && x < MaxDecodedIx; ch++, i += 4)
                    {
                        ref IMAState State = ref States[ch];
                        State.PredictedSample = EncodedInitSamples[i / 2] = DecodedSamples[x++];
                        EncodedBytes[i + 2] = (byte)State.StepIndex;
                    }
                    for (int j = 0; j < BlockSize; j++)
                    {
                        for (ushort ch = 0; ch < WAV.Header.Channels && x < MaxDecodedIx - 2; ch += 2)
                        {
                            EncodedBytes[i++] = Encode(DecodedSamples[x++], DecodedSamples[x++], ref States[ch], ref States[IsMono ? 0 : ch + 1]);
                        }
                    }
                }
            }
            else if (WAV.Header.Channels == 1)
            {
                IMAState State = new();
                for (int i = 0, x = 0; i < EncodedSize; i++)
                {
                    EncodedBytes[i] = Encode(DecodedSamples[x++], DecodedSamples[x++], ref State, ref State);
                }
            }
            else if (WAV.Header.Channels == 2)
            {
                IMAState Left = new(), Right = new();
                for (int i = 0, x = 0; i < EncodedSize; i++)
                {
                    EncodedBytes[i] = Encode(DecodedSamples[x++], DecodedSamples[x++], ref Left, ref Right);
                }
            }
            else if (WAV.Header.Channels == 4)
            {
                IMAState Ch0 = new(), Ch1 = new(), Ch2 = new(), Ch3 = new();
                for (int BI = 0, x = 0; BI < EncodedSize; BI += MusicBlockSize)
                {
                    int LastIPerTrack = BI + Math.Min(MusicBlockSize, EncodedSize - BI) - HalfBlockSize;
                    for (int i = BI; i < LastIPerTrack; i++)
                    {
                        EncodedBytes[i] = Encode(DecodedSamples[x++], DecodedSamples[x++], ref Ch0, ref Ch1);
                        EncodedBytes[i + HalfBlockSize] = Encode(DecodedSamples[x++], DecodedSamples[x++], ref Ch2, ref Ch3);
                    }
                }
            }
            // else: not implemented, returns 0 array (silence)
            return EncodedBytes;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static byte Encode(short LowSample, short HighSample, ref IMAState LowCh, ref IMAState HighCh)
        {
            return (byte)((Encode(HighSample, ref HighCh) << 4) | Encode(LowSample, ref LowCh));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static byte Encode(short Sample, ref IMAState State)
        {
            short StepSize = StepSizes[State.StepIndex];
            int sampleDiff = Sample - State.PredictedSample;
            bool signed = sampleDiff < 0;
            if (signed) { sampleDiff = -sampleDiff; }
            // unlooped for faster performance (https://github.com/Sergeanur/XboxADPCM/blob/767dc2640f8de4ac1f4fd6badfd13a402b3d1713/XboxADPCM/ImaADPCM.cpp#L50)
            sampleDiff = (sampleDiff << 2) / StepSize;
            if (sampleDiff > 7) { sampleDiff = 7; }
            byte EncodedSample = (byte)(sampleDiff | (signed ? 8 : 0));
            int diff = ((sampleDiff * StepSize) >> 2) + (StepSize >> 3);
            // seemingly from absolute to relative (adding difference)
            State.PredictedSample = Math.Clamp(State.PredictedSample + (signed ? -diff : diff), short.MinValue, short.MaxValue);
            State.StepIndex = Math.Clamp(State.StepIndex + StepIndices[EncodedSample & 7], byte.MinValue, MaxStepIndex); // get from half sized StepIndices
            return EncodedSample;
            // Original code, more truncation (see above)
            //if (sampleDiff >= StepSize)
            //{
            //    EncodedSample |= 4;
            //    sampleDiff -= StepSize;
            //    diff += StepSize;
            //}
            //StepSize >>= 1;
            //if (sampleDiff >= StepSize)
            //{
            //    EncodedSample |= 2;
            //    sampleDiff -= StepSize;
            //    diff += StepSize;
            //}
            //StepSize >>= 1;
            //if (sampleDiff >= StepSize)
            //{
            //    EncodedSample |= 1;
            //    diff += StepSize;
            //}
        }
    }

    public static class TemporaryPlayer
    {
        // https://learn.microsoft.com/en-us/dotnet/csharp/advanced-topics/interop/how-to-use-platform-invoke-to-play-a-wave-file
        [DllImport("winmm.DLL", EntryPoint = "PlaySound", SetLastError = true, CharSet = CharSet.Unicode, ThrowOnUnmappableChar = true)]
        private static extern bool PlaySound(string szSound, IntPtr hMod, PlaySoundFlags flags);

        [Flags]
        private enum PlaySoundFlags
        {
            /// <summary>play synchronously (default)</summary>
            SND_SYNC = 0x0000,
            /// <summary>play asynchronously</summary>
            SND_ASYNC = 0x0001,
            /// <summary>silence (!default) if sound not found</summary>
            SND_NODEFAULT = 0x0002,
            /// <summary>pszSound points to a memory file</summary>
            SND_MEMORY = 0x0004,
            /// <summary>loop the sound until next sndPlaySound</summary>
            SND_LOOP = 0x0008,
            /// <summary>don’t stop any currently playing sound</summary>
            SND_NOSTOP = 0x0010,
            /// <summary>Stop Playing Wave</summary>
            SND_PURGE = 0x40,
            /// <summary>don’t wait if the driver is busy</summary>
            SND_NOWAIT = 0x00002000,
            /// <summary>name is a registry alias</summary>
            SND_ALIAS = 0x00010000,
            /// <summary>alias is a predefined id</summary>
            SND_ALIAS_ID = 0x00110000,
            /// <summary>name is file name</summary>
            SND_FILENAME = 0x00020000,
            /// <summary>name is resource name or atom</summary>
            SND_RESOURCE = 0x00040004
        }

        public static async Task<bool> Play(string ExtractDir, UISample SampleInfo)
        {
            if (SampleInfo.Flags.HasFlag(ZsndProperties.SampleF.AmbientEmbedded)) { return false; } // seems to play default (silence), so report incompatibility
            string? PlayPath = await ZsndConvert.To(SampleInfo, ExtractDir);
            return PlayPath is not null
                && PlaySound(PlayPath, IntPtr.Zero, PlaySoundFlags.SND_FILENAME | PlaySoundFlags.SND_ASYNC);
        }

    }
}
