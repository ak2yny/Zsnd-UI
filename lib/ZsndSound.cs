using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using static Zsnd_UI.lib.ZsndProperties;

namespace Zsnd_UI.lib
{
    /// <summary>
    /// Zsnd main representaion using lists.
    /// </summary>
    public class UIRoot
    {
        public ZsndPlatform Platform { get; set; }
        public List<UISound> Sounds { get; set; }
        public List<UISample> Samples { get; set; }

        private readonly HashSet<string> filenames = [];
        private readonly bool[] isIndexDuplicate;

        public UIRoot() { Platform = new("PC"); Sounds = []; Samples = []; isIndexDuplicate = []; }

        public UIRoot(JsonRoot JSON, FileEvents FE)
        {
            Platform = new(JSON.Platform);
            Sounds = new(JSON.Sounds.Count);
            Samples = JSON.Samples;
            isIndexDuplicate = [];
            for (int i = 0; i < JSON.Sounds.Count; i++)
            {
                UISoundBase S = JSON.Sounds[i];
                Sounds.Add(new(Hashing.EnsureHash(S.Hash, FE), S.Flags, JSON.Samples[S.SampleIndex]));
            }
            for (int i = 0; i < Samples.Count; i++)
            {
                UISample S = Samples[i];
                S.Name = System.IO.Path.GetFileName(S.File);
            }
        }

        public UIRoot(ZsndPlatform Plat, List<UISample> Sa, List<UISound> So, bool[]? dupes = null)
        {
            Platform = Plat;
            Sounds = So;
            Samples = Sa;
            isIndexDuplicate = dupes ?? [];
        }

        public void AddSound(uint hash, byte flags, int sampleIndex, FileEvents FE)
        {
            UISample sample = Samples[sampleIndex];
            UISound sound = new(Hashing.ToStr(hash, sample.Name!, FE), flags, sample);
            if (!isIndexDuplicate[sampleIndex])
            {
                isIndexDuplicate[sampleIndex] = true;
                if (!filenames.Add(sample.Name!))
                { sample.Name = $"{Functions.ZsndPath.GetParentPath(sound.Hash)}/{sample.Name}"; }
                // Could also add new sample.Name for safety.
            }
            Sounds.Add(sound);
        }
    }
    /// <summary>
    /// Zsnd main JSON representaion using lists. Note: not observable for performance reason.
    /// </summary>
    public class JsonRoot
    {
        public string Platform { get; set; }
        public List<UISoundBase> Sounds { get; set; }
        public List<UISample> Samples { get; set; }

        public JsonRoot() { Platform = "PC"; Sounds = []; Samples = []; }

        public JsonRoot(UIRoot Root)
        {
            Platform = Root.Platform.String;
            Sounds = [.. Root.Sounds];
            Samples = Root.Samples;
        }
    }
    /// <summary>
    /// Zsnd UI Sample <see cref="ObservableObject"/>
    /// </summary>
    public partial class UISample : ObservableObject
    {
        [JsonIgnore]
        public string? Name { get; set; }

        public string? File { get; set; }
        //[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public uint Format { get; set; }
        [ObservableProperty]
        [JsonPropertyName("sample_rate")]
        public partial uint SampleRate { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public SampleF Flags { get; set; }

        [JsonIgnore]
        public uint Offset { get; set; }
        [JsonIgnore]
        public uint Size { get; set; }
        //[JsonIgnore]
        //public string? FormatType => System.IO.Path.GetExtension(Name); // only if !Plat.IsMicrosoft

        public UISample() { }
        public UISample(string file, uint format, uint sr, ushort flags, uint offset, uint size, string source)
        {
            Name = file;
            Format = format;
            SampleRate = sr;
            Flags = (SampleF)flags;
            File = source;
            Offset = offset;
            Size = size;
        }
    }
    /// <summary>
    /// Zsnd UI Sound Base <see cref="ObservableObject"/>
    /// </summary>
    public partial class UISoundBase(string hash, byte flags) : ObservableObject
    {
        [ObservableProperty]
        [JsonConverter(typeof(HashStringConverter))]
        public partial string Hash { get; set; } = hash;

        [JsonPropertyName("sample_index")]
        public int SampleIndex { get; set; } = -1;

        [ObservableProperty]
        public partial byte Flags { get; set; } = flags;

        [ObservableProperty]
        [JsonIgnore]
        public partial bool F1 { get; set; } = (flags & 1) == 1;

        [ObservableProperty]
        [JsonIgnore]
        public partial bool F2 { get; set; } = (flags & 2) == 2;

        [ObservableProperty]
        [JsonIgnore]
        public partial bool F3 { get; set; } = (flags & 4) == 4;

        [ObservableProperty]
        [JsonIgnore]
        public partial bool F4 { get; set; } = (flags & 8) == 8;

        [ObservableProperty]
        [JsonIgnore]
        public partial bool F5 { get; set; } = (flags & 16) == 16;

        [ObservableProperty]
        [JsonIgnore]
        public partial bool F6 { get; set; } = (flags & 32) == 32;

        [ObservableProperty]
        [JsonIgnore]
        public partial bool F7 { get; set; } = (flags & 64) == 64;

        [ObservableProperty]
        [JsonIgnore]
        public partial bool F8 { get; set; } = (flags & 128) == 128;

        partial void OnF1Changed(bool value) { Flags = (byte)(value ? Flags | 1 : Flags & ~1); }
        partial void OnF2Changed(bool value) { Flags = (byte)(value ? Flags | 2 : Flags & ~2); }
        partial void OnF3Changed(bool value) { Flags = (byte)(value ? Flags | 4 : Flags & ~4); }
        partial void OnF4Changed(bool value) { Flags = (byte)(value ? Flags | 8 : Flags & ~8); }
        partial void OnF5Changed(bool value) { Flags = (byte)(value ? Flags | 16 : Flags & ~16); }
        partial void OnF6Changed(bool value) { Flags = (byte)(value ? Flags | 32 : Flags & ~32); }
        partial void OnF7Changed(bool value) { Flags = (byte)(value ? Flags | 64 : Flags & ~64); }
        partial void OnF8Changed(bool value) { Flags = (byte)(value ? Flags | 128 : Flags & ~128); }
        /// <summary>
        /// Switch the flag to the 31 representation, keeping the lower 5 flags as they are and disabling the high 3. Nees better descriptions after knowing what the flags do.
        /// </summary>
        //public void To31()
        //{
        //    F6 = false;
        //    F7 = false;
        //    F8 = false;
        //}
    }
    /// <summary>
    /// Zsnd UI Sound Entry <see cref="ObservableObject"/>
    /// </summary>
    public partial class UISound : UISoundBase
    {
        [ObservableProperty]
        [JsonIgnore]
        public partial UISample Sample { get; set; }

        [ObservableProperty]
        internal partial bool Loop { get; set; }

        [ObservableProperty]
        [JsonIgnore]
        public partial string? CharFile { get; set; }

        [JsonIgnore]
        public bool PendingChanges { get; set; }

        public UISound(string hash, byte flags) : base(hash, flags) { Sample = new(); }
        public UISound(string hash, byte flags, UISample sample) : base(hash, flags)
        {
            Sample = sample;
        }

        partial void OnSampleChanged(UISample value)
        {
            if (value is null) { return; }
            Loop = value.Flags.HasFlag(SampleF.Loop);
        }

        partial void OnLoopChanged(bool value)
        {
            if (value == Sample.Flags.HasFlag(SampleF.Loop)) { return; }
            Sample.Flags = value ? Sample.Flags | SampleF.Loop : Sample.Flags & ~SampleF.Loop;
            OnPropertyChanged(nameof(Sample));
        }

        partial void OnCharFileChanged(string? oldValue, string? newValue)
        {
            if (oldValue is null || newValue is null) { return; }
            int i = Hash.IndexOf('/') + 1; if (i == 0) { return; }
            int s = Hash.IndexOf('_', i);
            int Length = i + newValue.Length + (s == -1 ? 0 : Hash.Length - s);
            Hash = string.Create(Length, (Hash, newValue, i, s), (hash, args) =>
            {
                var (o, file, i1, i2) = args;
                ReadOnlySpan<char> old = o;
                old[..i1].CopyTo(hash);
                file.AsSpan().CopyTo(hash.Slice(i1));
                if (i2 != -1) { old.Slice(i2).CopyTo(hash.Slice(i1 + file.Length)); }
            });
        }
    }
    /*
    /// <summary>
    /// X_voice info class with the required details for Zsnd files (<see cref="ObservableObject"/>).
    /// </summary>
    public partial class XVSound : UISoundBase
    {
        [ObservableProperty]
        public partial Events.MenuPrefix? Pref { get; set; }

        [ObservableProperty]
        public partial string? IntName { get; set; }
        /// <summary>
        /// Updates the <see cref="UISoundBase.Hash"/> based on the changed <paramref name="newValue"/> and <see cref="IntName"/> (must always be set first).
        /// </summary>
        partial void OnPrefChanged(Events.MenuPrefix? oldValue, Events.MenuPrefix? newValue)
        {
            if (newValue is null || IntName is null || oldValue is null) { return; }
            if (newValue is Events.MenuPrefix.TEAM)
            {
                // If no internal name, old stays unchanged. Hash must be managed externally in such cases.
                if (IntName.StartsWith("TEAM_BONUS_", StringComparison.OrdinalIgnoreCase)) { IntName = (IntName[11..]); }
                else if (IntName.StartsWith("BONUS_", StringComparison.OrdinalIgnoreCase)) { IntName = (IntName[6..]); }
            }
            Hash = $"COMMON/{Events.MenuPrefixes[((int)newValue)]}{IntName}";
        }

        public XVSound() { }

        public XVSound(JsonSound value) : base(value.Hash, value.Sample_index, (byte)value.Flags)
        {
            // value must already have been processed with Hashing.EnsureHash(value.Hash)
            string[] HE = value.Hash.Split('/')[^1].Split('_', 2);
            if (Enum.TryParse(HE[0], out Events.MenuPrefix HEP)) { Pref = HEP; }
            if (HE.Length > 1) { IntName = HE[1]; } // After (Important to avoid OnPrefChanged)
            _ = Pref is Events.MenuPrefix.AN or Events.MenuPrefix.BREAK
                && Lists.XVInternalNames.Add(IntName);
        }

        public static implicit operator XVSound(JsonSound value) => new(value);
    }
    */

    public class HashStringConverter : JsonConverter<string>
    {
        public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.TokenType switch
            {
                JsonTokenType.String => reader.GetString(),
                JsonTokenType.Number => System.Text.Encoding.Latin1.GetString(reader.ValueSpan),
                _ => throw new JsonException($"Unexpected token parsing hash. Token: {reader.TokenType}"),
            };
        }

        public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
        {
            if (uint.TryParse(value, out uint number)) { writer.WriteNumberValue(number); } else { writer.WriteStringValue(value); }
        }
    }
}
