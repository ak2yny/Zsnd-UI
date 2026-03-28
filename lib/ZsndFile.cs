using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Zsnd_UI.Functions;

namespace Zsnd_UI.lib
{
    internal partial class ZsndFile : ObservableObject
    {
        private static readonly Microsoft.Windows.ApplicationModel.Resources.ResourceLoader _resourceLoader = new();

        private Microsoft.UI.Dispatching.DispatcherQueue DispatcherQueue { get; } = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();

        internal NotifyTaskCompletion<bool> TaskInfo { get; private set; }

        private System.Threading.CancellationTokenSource? _cts;

        internal string? ErrorSubtitle => TaskInfo.ShowErrorSubtitle
            ? string.Format(_resourceLoader.GetString("FileErrorFailedSubTitle"), ZsndFilePath)
            : null;

        [ObservableProperty]
        internal partial bool PendingChanges { get; set; }

        [ObservableProperty]
        internal partial string Name { get; set; }

        [ObservableProperty]
        internal partial string ExtractedDirName { get; set; } // WIP: Saveable?

        internal string ExtractedDir
        {
            get
            {
                string path = Path.Combine(JsonDir, ExtractedDirName);
                return File.Exists(path) ? path : Directory.CreateDirectory(path).FullName;
            }
        }

        [ObservableProperty]
        internal partial string ZsndFilePath { get; private set; }

        [ObservableProperty]
        internal partial string JsonFilePath { get; private set; }

        [ObservableProperty]
        internal partial string JsonDir { get; private set; }

        private string? BaseName { get; set; }

        internal List<UISound> Sounds = [];
        internal System.Collections.ObjectModel.ObservableCollection<UISound> FilteredSounds { get; set; } = [];

        internal System.Collections.ObjectModel.ObservableCollection<UISample> Samples { get; set; } = [];

        [ObservableProperty]
        internal partial bool SampleEditorOpen { get; set; }

        [ObservableProperty]
        internal partial bool SampleEditorClosed { get; private set; } = true;

        [ObservableProperty]
        internal partial int SelectedSampleIndex { get; set; }

        [ObservableProperty]
        internal partial ZsndPlatform PlatformInfo { get; private set; } = new("PC"); // Displays while loading

        [ObservableProperty]
        internal partial UISound? SelectedSound { get; set; }
        // Selected Sound Hash:
        [ObservableProperty]
        internal partial int IndexCategory { get; set; }

        [ObservableProperty]
        internal partial int IndexMaster { get; set; }

        [ObservableProperty]
        internal partial int IndexVoice { get; set; }

        [ObservableProperty]
        internal partial int IndexMenu { get; set; }

        [ObservableProperty]
        internal partial int IndexMusic { get; set; }

        [ObservableProperty]
        internal partial bool CharacterIsV { get; set; }

        [ObservableProperty]
        internal partial bool CharacterIsM { get; set; } = true;

        [ObservableProperty]
        internal partial bool CharacterEvent { get; set; }

        [ObservableProperty]
        internal partial bool MenuEvent { get; set; }

        [ObservableProperty]
        internal partial bool MusicEvent { get; set; }

        [ObservableProperty]
        internal partial string? MenuName { get; set; }

        internal readonly FileEvents FEvents = new();

        internal ZsndFile()
        {
            Name = ExtractedDirName = "NEWFILE";
            SetJsonDir(ViewModels.Shared.UI.ExtractedAssetsPath);
            ZsndFilePath = "";
            TaskInfo = new NotifyTaskCompletion<bool>(Task.FromResult(true));
        }

        internal ZsndFile(string FilePath, string Extension, string DisplayName) { LoadFile(FilePath, Extension, DisplayName); }

        [MemberNotNull(nameof(JsonDir), nameof(JsonFilePath))]
        private void SetJsonFilePath(string value) { JsonDir = Path.GetDirectoryName(value)!; JsonFilePath = value; }

        [MemberNotNull(nameof(JsonDir), nameof(JsonFilePath))]
        private void SetJsonDir(string value) { JsonFilePath = Path.Combine(value, $"{Name}.json"); JsonDir = value; }
        // Task is expected to catch exceptions
        private async Task<bool> LoadFileAsync(bool isJson)
        {
            _cts = new(); System.Threading.CancellationToken Token = _cts.Token;
            UIRoot result = await Task.Run(async () =>
            {
                if (isJson)
                {
                    UIRoot root = await Cmd.LoadJson(JsonFilePath, FEvents, Token);
                    return root;
                }
                else
                {
                    return Cmd.LoadZsnd(ZsndFilePath, FEvents, Token);
                }
            });
            return DispatcherQueue.TryEnqueue(() =>
            {
                Sounds = result.Sounds;
                PlatformInfo = result.Platform;
                // Must add files one by one, because UI is currently showing empty list.
                // If initialization can be delayed, we could use FilteredSounds = FilteredSounds is null ? new(Sounds) : ;
                for (int i = 0; i < Sounds.Count; i++) { FilteredSounds.Add(Sounds[i]); }
                for (int i = 0; i < result.Samples.Count; i++) { Samples.Add(result.Samples[i]); }
            });
        }

        [MemberNotNull(nameof(Name), nameof(JsonDir), nameof(JsonFilePath), nameof(ZsndFilePath),
            nameof(ExtractedDirName), nameof(TaskInfo))]
        internal void LoadFile(string FilePath, string Extension, string DisplayName)
        {
            bool isJson = Extension.Equals(".json", StringComparison.OrdinalIgnoreCase);
            Name = ExtractedDirName = DisplayName;
            ZsndFilePath = isJson ? $"{FilePath[..^Extension.Length]}.zs{(FEvents.MFirst ? 'm' : 's')}" : FilePath;
            if (isJson) { SetJsonFilePath(FilePath); } else { SetJsonDir(ViewModels.Shared.UI.ExtractedAssetsPath); }
            TaskInfo = new NotifyTaskCompletion<bool>(LoadFileAsync(isJson));
        }

        private UIRoot ToRoot()
        {
            HashSet<UISample> Used = [];
            Dictionary<UISample, int> SampleToIndex = [];
            for (int i = 0; i < Sounds.Count; i++) { _ = Used.Add(Sounds[i].Sample); }
            for (int i = 0; i < Samples.Count;)
            {
                UISample s = Samples[i];
                if (Used.Contains(s)) { SampleToIndex[s] = i++; }
                else { Samples.RemoveAt(i); }
            }
            for (int i = 0; i < Sounds.Count; i++)
            {
                UISound So = Sounds[i];
                So.SampleIndex = SampleToIndex[So.Sample];
            }
            return new(PlatformInfo, [.. Samples], Sounds); // ToList(), merely references
        }

        internal void SaveJson()
        {
            if (!PendingChanges || TaskInfo.IsNotCompleted) { return; }
            TaskInfo.Run(SaveJson(JsonFilePath), () => OnPropertyChanged(nameof(ErrorSubtitle)));
        }

        private async Task<bool> SaveJson(string FilePath, int ExtLen = 0)
        {
            if (!PlatformInfo.IsValid) { throw new PlatformNotSupportedException($"Platform '{PlatformInfo.String}' is not supported."); }
            _cts = new();
            await Cmd.ExtractZsnd(ZsndFilePath, ExtractedDir, Samples, true, _cts.Token);
            await Task.Run(() =>
            {
                for (int i = 0; i < Samples.Count; i++)
                {
                    UISample S = Samples[i];
                    S.File = Path.Combine(ExtractedDirName, S.Name!);
                    S.Offset = 0;
                }
            });
            await Cmd.SaveJson(FilePath, ToRoot(), _cts.Token);
            return DispatcherQueue.TryEnqueue(() =>
            {
                // WIP: Handle success message?
                if (ExtLen != 0) { ZsndFilePath = $"{FilePath[..^ExtLen]}.zs{(FEvents.MFirst ? 'm' : 's')}"; }
                PendingChanges = false;
			});
        }

        private async Task<bool> SaveZsnd(string FilePath)
        {
            // WIP: Too slow, especially first loop (possibly connected to random replace issue?). Progress ring doesn't show.
            await Task.Yield();
            _cts = new();
            await Cmd.WriteZsnd(FilePath, ToRoot(), _cts.Token).WaitAsync(TimeSpan.FromMinutes(1));
            _ = DispatcherQueue.TryEnqueue(() =>
            {
                // WIP: Possibly show success message: show; await Task.Delay(5000); hide
                //      Except, if no tab is selected
                //SuccessInfo.Message = SuccMsg;
                //SuccessInfo.IsOpen = true;
                //await System.Threading.Tasks.Task.Delay(System.TimeSpan.FromSeconds(3));
                //SuccessInfo.IsOpen = false;
                PendingChanges = false;
            });
            return true;
        }

        internal void SaveZsnd()
        {
            if (!PendingChanges || TaskInfo.IsNotCompleted || !ZsndPath.Backup(ZsndFilePath)) { return; }
            TaskInfo.Run(SaveZsnd(ZsndFilePath), () => OnPropertyChanged(nameof(ErrorSubtitle)));
        }

        internal void SaveFile(string FilePath, string Extension, string DisplayName)
        {
            Name = DisplayName;
            if (Extension.Equals(".json", StringComparison.OrdinalIgnoreCase))
            {
                SetJsonFilePath(FilePath);
                TaskInfo.Run(SaveJson(FilePath, Extension.Length), () => OnPropertyChanged(nameof(ErrorSubtitle)));
            }
            else
            {
                ZsndFilePath = FilePath;
                TaskInfo.Run(SaveZsnd(FilePath), () => OnPropertyChanged(nameof(ErrorSubtitle)));
            }
        }

        private void AddSound(UISound Sound) { AddSound(Sound, FilteredSounds.Count); }
        /// <summary>
        /// Adds the <paramref name="Sound"/> to the <see cref="FilteredSounds"/> collection and the <see cref="Sounds"/> list.
        /// </summary>
        internal void AddSound(UISound Sound, int index)
        {
            if (TaskInfo.IsNotCompleted) { return; }
            FilteredSounds.Insert(index, Sound);
            Sounds.Add(Sound);
            PendingChanges = true;
        }
        /// <summary>
        /// Adds a new sound to the <see cref="FilteredSounds"/> collection and the <see cref="Sounds"/> list. Uses the properties from the <see cref="SelectedSound"/>.
        /// </summary>
        internal void AddSound() { AddSound(SelectedSound is null ? new("", 0xFF) : new(SelectedSound.Hash, SelectedSound.Flags) { Loop = SelectedSound.Loop }); }
        /// <summary>
        /// Adds a new audio <paramref name="Sample"/> to the (WIP: place to extract, headerless + normal), converting and storing the file as needed.
        /// </summary>
        /// <returns>The newly added <see cref="UISample"/> if successful and not in samples list mode; otherwise, <see langword="null"/>.</returns>
        internal UISample? AddSample(Windows.Storage.StorageFile Sample)
        {
            // WIP: Improve headerless/with-header management and add button to convert to headerless
            if (TaskInfo.IsNotCompleted) { return null; }
            //FileIncompatible.IsOpen = FileMaxReached.IsOpen = false;
            if (Samples.Count > 0xFFFF)
            {
                //FileMaxReached.IsOpen = true;
                return null;
            }
            // WIP: Add option to use source file or copy?
            string extDir = ExtractedDir;
            UISample SampleInfo = new() { Name = Sample.Name };
            try
            {
                // Might need to add support for WAV to other formats (or report) - WIP?
                // WIP: Replace? Option? Option to use source? Must update Name, if backup.
                // Add SampleInfo, except if headerless fmt that's already headerless (no info in header)
                Span<byte> ConvertedFileBuffer = ZsndConvert.From(Sample.FileType, Sample.Path, SampleInfo, PlatformInfo);
                if (ConvertedFileBuffer.Length != 0) // headerless fmt with header now stripped
                {
                    File.WriteAllBytes(ZsndPath.GetHeaderlessPath(extDir, SampleInfo.Name), ConvertedFileBuffer);
                }
                else
                {
                    if (PlatformInfo.IsHeaderless)
                    {
                        SampleInfo.Format = 106;
                        SampleInfo.SampleRate = 22050;
                    }
                    string extFile = PlatformInfo.IsHeaderless
                        ? ZsndPath.GetHeaderlessPath(extDir, SampleInfo.Name)
                        : Path.Combine(extDir, Sample.Name);
                    //await ZsndConvert.To(SampleInfo, Sample.Path, ".wav");
                    if (Sample.Path != extFile) { File.Copy(Sample.Path, extFile, true); }
                    // or SampleInfo.File = Sample.Path;
                }
                Samples.Add(SampleInfo);
                return SampleEditorOpen ? null : SampleInfo;
            }
            catch
            {
                // WIP: Error reporting
                // Also happens if the source and destination are identical (source stream is not yet closed)
                //FileIncompatible.IsOpen = true;
                return null;
            }

        }
        /// <summary>
        /// Adds a new audio <paramref name="Sample"/> to <see cref="Samples"/> at <paramref name="index"/>, converting and storing the file as needed; adds a new sound, if the sample was added successfully.
        /// </summary>
        internal void AddSampleSound(Windows.Storage.StorageFile Sample, int index)
        {
            if (AddSample(Sample) is UISample sample)
            {
                byte flags = 0xFF;
                if (SelectedSound is UISound S)
                { flags = S.Flags; if (S.Loop) { sample.Flags |= ZsndProperties.SampleF.Loop; }; }
                AddSound(new(
                    Hashing.CharHashFromFile(Sample.DisplayName.ToUpperInvariant(), Name.ToUpperInvariant()),
                    flags, sample), index);
            }
        }

        internal async Task ExtractSamples(bool raw = false)
        {
            if (TaskInfo.IsNotCompleted || Samples.Count == 0) { return; }
            _cts = new();
            await Cmd.ExtractZsnd(ZsndFilePath, ExtractedDir, Samples, raw, _cts.Token);
        }

        internal void SortSounds(string SortTag)
        {
            if (TaskInfo.IsNotCompleted) { return; }
            if (SampleEditorOpen)
            {
                Samples.Sort(
                    SortTag == "source.asc"
                    ? static c => c.OrderBy(static s => s.File)
                    : SortTag == "source.desc"
                    ? static c => c.OrderByDescending(static s => s.File)
                    : SortTag == "name.asc"
                    ? static c => c.OrderBy(static s => s.Name)
                    : SortTag == "name.desc"
                    ? static c => c.OrderByDescending(static s => s.Name)
                    : SortTag == "sr.asc"
                    ? static c => c.OrderBy(static s => s.SampleRate)
                    : SortTag == "sr.desc"
                    ? static c => c.OrderByDescending(static s => s.SampleRate)
                    : SortTag == "flags.asc"
                    ? static c => c.OrderBy(static s => s.Flags)
                    : SortTag == "flags.desc"
                    ? static c => c.OrderByDescending(static s => s.Flags)
                    : static c => c.OrderBy(static s => s)); // never happens
            }
            else
            {
                FilteredSounds.Sort(
                    SortTag == "hash.asc"
                    ? static c => c.OrderBy(static s => s.Hash)
                    : SortTag == "hash.desc"
                    ? static c => c.OrderByDescending(static s => s.Hash)
                    : SortTag == "name.asc"
                    ? static c => c.OrderBy(static s => s.Sample.Name)
                    : SortTag == "name.desc"
                    ? static c => c.OrderByDescending(static s => s.Sample.Name)
                    : SortTag == "index.asc"
                    ? static c => c.OrderBy(static s => s.SampleIndex)
                    : SortTag == "index.desc"
                    ? static c => c.OrderByDescending(static s => s.SampleIndex)
                    : static c => c.OrderBy(static s => s)); // never happens
                ApplySoundOrder();
            }
        }

        internal void ApplySoundOrder() { if (Sounds.Count == FilteredSounds.Count) { Sounds = [.. FilteredSounds]; } }

        internal void Filter(string Filter)
        {
            TaskInfo.Run(SampleEditorOpen ? FilterSamples(Filter) : FilterSounds(Filter));
        }

        private Task<bool> FilterSamples(string Filter)
        {
            if (Filter.Length == 0)
            {
                SelectedSampleIndex = 0;
                return Task.FromResult(true);
            }
            return Task.Run(() =>
            {
                return DispatcherQueue.TryEnqueue(() =>
                {
                    for (int i = 0; i < Samples.Count; i++)
                    {
                        if (FilterMatches(Samples[i], Filter))
                        {
                            SelectedSampleIndex = i;
                            break;
                        }
                    }
                });
            });
            static bool FilterMatches(UISample s, string filter)
            {
                return s.Name is not null && (s.Name.Contains(filter, StringComparison.OrdinalIgnoreCase)
                    || (s.File is not null && s.File.Contains(filter, StringComparison.OrdinalIgnoreCase)));
            }
        }

        private Task<bool> FilterSounds(string Filter)
        {
            Func<UISound, bool> FilterMatches = Filter.Length == 0 ? (s => true) : (s =>
            {
                return s.Sample.Name is null
                    ? s.Hash.Contains(Filter, StringComparison.Ordinal)
                    : s.Hash.Contains(Filter, StringComparison.Ordinal)
                        || s.Sample.Name.AsSpan().Contains(Filter, StringComparison.OrdinalIgnoreCase);
            });
            return Task.Run(() =>
            {
                return DispatcherQueue.TryEnqueue(() =>
                {
                    for (int i = 0; i < Sounds.Count; i++)
                    {
                        UISound S = Sounds[i];
                        if (FilterMatches(S))
                        {
                            if (!FilteredSounds.Contains(S)) { FilteredSounds.Add(S); }
                        }
                        else { _ = FilteredSounds.Remove(S); }
                    }
                });
            });
        }
        /*
        private void FilterSounds(Func<UISound, bool> FilterMatches)
        {
            FilteredSounds.Clear();
            for (int s = 0; s < Sounds.Count; s++)
            {
                if (FilterMatches(Sounds[s])) { FilteredSounds.Add(Sounds[s]); }
            }
        }
        */
        internal void RemoveSelected()
        {
            if (TaskInfo.IsRunning) { return; }
            if (SelectedSound is UISound Sound && FilteredSounds.Remove(Sound)) { RemoveSampleSound(Sound); }
        }

        internal void RemoveSoundAt(int index)
        { RemoveSampleSound(FilteredSounds[index]); FilteredSounds.RemoveAt(index); }

        private void RemoveSampleSound(UISound Sound)
        {
            // Possibly make this a task, seems to be fast enough, though
            if (Sounds.Remove(Sound))
            {
                UISample Sample = Sound.Sample;
                for (int i = 0; i < Sounds.Count; i++) { if (Sounds[i].Sample == Sample) { return; } }
                _ = Samples.Remove(Sample);
            }
        }

        internal void DeleteSelectedSoundSampleFile()
        {
            if (TaskInfo.IsRunning || SelectedSound is null || SelectedSound.Sample.Name is null) { return; }
            string path = Path.Combine(ExtractedDir, SelectedSound.Sample.Name);
            try
            {
                if (path == SelectedSound.Sample.File)
                {
                    using FileStream fs = new(path, FileMode.Open, FileAccess.Read);
                    PCM_WAVE_Reader WAV = new(fs);
                    SelectedSound.Sample.SampleRate = WAV.Header.SampleRate;
                }
                else { File.Delete(path); }
            }
            catch { }
        }

        partial void OnNameChanged(string value)
        {
            string NameUpper = value.ToUpperInvariant();
            BaseName = value.Length > 2 && value[^2] is '_' ? NameUpper[..^2] : NameUpper;
            FEvents.Update(NameUpper, BaseName);
        }

        private void SelectedSoundPropertiesChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e) { PendingChanges = true; }

        internal void HashToEvents() { if (SelectedSound is not null) { SoundToEvents(SelectedSound, true); } }

        private void SoundToEvents(UISound sound, bool force = false)
        {
            // Seems to be fast enough, UI block not noticeable, using workaround (instead of TaskInfo)
            TaskInfo.IsRunning = true;
            int le = sound.Hash.Length;
            int ix = sound.Hash.IndexOf('/'); if (ix == -1) { ix = le - 1; }
            _ = Enum.TryParse(sound.Hash[..ix], out Events.Category C); ix++;
            int c = (int)C; IndexCategory = c < Events.Categories.Length ? c : 0;
            switch (C)
            {
                case Events.Category.CHAR or Events.Category.CHARACTER:
                    int ni = sound.Hash.LastIndexOf('/', le - 1, le - ix); if (ni == -1) { ni = ix; }
                    CharacterIsV = sound.Hash[ni - 1] is 'V'; // '/' if no second/last slash found
                    if (force || sound.CharFile is null)
                    { sound.CharFile = ix < ni - 2 ? sound.Hash[ix..(ni - 2)] : BaseName; }
                    ni++;
                    if (CharacterIsV) { IndexVoice = Events.Voice.IndexOf(sound.Hash[ni..]); }
                    else { IndexMaster = Events.Master.IndexOf(sound.Hash[ni..]); }
                    break;
                case Events.Category.COMMON:
                    ReadOnlySpan<char> sc = sound.Hash.AsSpan();
                    Events.MenuPrefix p = le > 23 && sc[13] is 'C' && sc.Slice(ix, 16).SequenceEqual(Events.MenuPrefixes[4]) // or sc.Equals("MENUS/CHARACTER/", StringComparison.OrdinalIgnoreCase);
                        ? le > 26 && sc[23] is 'A' && sc[24] is 'N'
                            ? Events.MenuPrefix.AN : le > 29 && sc.Slice(23, 5).SequenceEqual("BREAK")
                            ? Events.MenuPrefix.BREAK
                            : Events.MenuPrefix.CHARACTER
                        : le > 18 && sc[ix] is 'T' && sc.Slice(ix, 11).SequenceEqual(Events.MenuPrefixes[1])
                            ? Events.MenuPrefix.TEAM
                        : le > 19 && Enum.TryParse(sc[13..(sc[17] is '/' ? 17 : sc[18] is '/' ? 18 : le)],
                            out Events.MenuPrefix op) ? op // doesn't check sc.Slice(ix, 6).SequenceEqual("MENUS/")
                            : Events.MenuPrefix.OTHER;
                    IndexMenu = (int)p;
                    MenuName = sound.Hash[(7 + Events.MenuPrefixes[IndexMenu].Length)..];
                    break;
                case Events.Category.MUSIC: // Possible WIP: Other events?
                    ReadOnlySpan<char> sm = sound.Hash.AsSpan();
                    IndexMusic = le > 12 && sm.Slice(ix, 4).SequenceEqual("CUES")
                        ? sm[11] is 'I' && sm[12] is 'N'
                            ? 3 : 4
                        : le > 8 && sm[^2] is '_'
                            ? Events.MusSfxs.IndexOf(sm[^1])
                            : 6;
                    break;
                case Events.Category.VOICE: // Conversation voices, do they need an extra box?
                default:
                    if (string.Equals(sound.Hash, "STATSCREEN/AMB", StringComparison.OrdinalIgnoreCase)) // rare case
                    { IndexCategory = 4; IndexMusic = 5; }
                    break;
            }
            TaskInfo.IsRunning = false;
        }

        partial void OnSelectedSoundChanged(UISound? oldValue, UISound? newValue)
        {
            if (newValue is null || newValue.Hash.Length == 0) { return; }
            SoundToEvents(newValue);
            oldValue?.PropertyChanged -= SelectedSoundPropertiesChanged; // only track changes on the currently selected
            newValue.PropertyChanged += SelectedSoundPropertiesChanged;
        }

        partial void OnCharacterIsVChanged(bool value)
        {
            CharacterIsM = !value;
            if (TaskInfo.IsRunning) { return; }
            if (value) { CharEventsChanged('V', IndexVoice, Events.Voice); }
            else { CharEventsChanged('M', IndexMaster, Events.Master); }
        }

        internal void CharEventsChanged(char type, int index, string[] events)
        {
            if (TaskInfo.IsRunning || SelectedSound is null || index == -1) { return; }
            SelectedSound.Hash = $"{Events.Categories[IndexCategory]}/{SelectedSound.CharFile}_{type}/{events[index]}";
        }

        partial void OnIndexCategoryChanged(int oldValue, int newValue)
        {
            if (newValue != -1 && SelectedSound is not null)
            {
                switch (newValue)
                {
                    case 1 or 2: MenuEvent = MusicEvent = !(CharacterEvent = true); break;
                    case 3: CharacterEvent = MusicEvent = !(MenuEvent = true); break;
                    case 4: CharacterEvent = MenuEvent = !(MusicEvent = true); break;
                    case 5 or 0:
                    default: CharacterEvent = MenuEvent = MusicEvent = false; break;
                }
                if (TaskInfo.IsRunning) { return; }
                string end = oldValue == 0 ? SelectedSound.Hash
                    : SelectedSound.Hash[(SelectedSound.Hash.IndexOf('/') + 1)..];
                SelectedSound.Hash = newValue == 0 ? end : $"{Events.Categories[newValue]}/{end}";
            }
        }

        partial void OnIndexMasterChanged(int value) => CharEventsChanged('M', value, Events.Master);

        partial void OnIndexVoiceChanged(int value) => CharEventsChanged('V', value, Events.Voice);

        partial void OnIndexMenuChanged(int value)
        {
            if (TaskInfo.IsRunning || SelectedSound is null || MenuName is null || value == -1) { return; }
            if (value == 1)
            {
                if (MenuName.StartsWith("TEAM_BONUS_", StringComparison.OrdinalIgnoreCase)) { MenuName = (MenuName[11..]); }
                else if (MenuName.StartsWith("BONUS_", StringComparison.OrdinalIgnoreCase)) { MenuName = (MenuName[6..]); }
            }
            SelectedSound.Hash = $"COMMON/{Events.MenuPrefixes[value]}{MenuName}";
        }

        partial void OnIndexMusicChanged(int value)
        {
            if (TaskInfo.IsRunning || SelectedSound is null || value == -1) { return; }
            if (value == 6 && SelectedSound.Hash.StartsWith("MUSIC/", StringComparison.OrdinalIgnoreCase)) { return; }
            SelectedSound.Hash = value == 6 ? $"MUSIC/{SelectedSound.Hash}" : Events.Music[value];
        }

        partial void OnMenuNameChanged(string? value)
        {
            OnIndexMenuChanged(IndexMenu); // WIP: MenuName might not be value, yet.
        }

        partial void OnSampleEditorOpenChanged(bool value)
        {
            SampleEditorClosed = !value;
        }

        internal void CancelOperations() { _cts?.Cancel(); }

        public override string ToString() => Name;
    }
}
