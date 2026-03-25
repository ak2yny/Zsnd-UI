using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;
using System.Linq;
using Windows.ApplicationModel.DataTransfer;
using Zsnd_UI.Functions;
using Zsnd_UI.lib;

namespace Zsnd_UI.Controls
{
    internal sealed partial class Tab_SoundsView : UserControl
    {
        public static readonly DependencyProperty FileProperty = DependencyProperty.Register(nameof(File), typeof(ZsndFile), typeof(Tab_SoundsView), new PropertyMetadata(null));
        internal ZsndFile File { get => (ZsndFile)GetValue(FileProperty); set => SetValue(FileProperty, value); }

        private readonly DispatcherTimer _searchTimer = new() { Interval = TimeSpan.FromMilliseconds(300) };

        internal Tab_SoundsView()
        {
            InitializeComponent();
            double CBWT = 186;
            foreach (ICommandBarElement? command in SearchBar.PrimaryCommands)
            {
                if (command is FrameworkElement element)
                {
                    element.Measure(new Windows.Foundation.Size(double.PositiveInfinity, double.PositiveInfinity));
                    CBWT += element.DesiredSize.Width;
                }
            }
            CommandBarWidthThreashold.MinWindowWidth = CBWT;
            _searchTimer.Tick += SearchTimer_Tick;
        }

        private void SoundsView_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            if (DataContext is ZsndFile file) { File = file; }
            args.Handled = true;
        }
        // WIP: Possibly add stop button to keep user informed about playing sounds
        //      When extracting, we might want to ask for confirmation, if the extraction directory already exists
        //      When adding samples (manually) the exact same sample might already be in the list (UISample or just same source/file)
        //      Add support to extract selected
        //      Might add a batch extractor in the future
        //      Add support to convert sounds

        private void Filter_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            _searchTimer.Stop(); // Stop previous searches
            if (File.Sounds.Count == 0) { return; }
            _searchTimer.Start();
        }

        private async void SearchTimer_Tick(object? sender, object e)
        {
            _searchTimer.Stop();
            File.Filter(Filter.Text);
            if (File.SampleEditorOpen) { _ = DispatcherQueue.TryEnqueue(() => SampleListView.ScrollIntoView(SampleListView.SelectedItem)); }
        }

        private void Filter_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is DependencyObject suggestionBox
                && Util.FindFirstChild<TextBox>(suggestionBox) is TextBox textBox)
            {
                textBox.CharacterCasing = CharacterCasing.Upper;
            }
        }
        /// <summary>
        /// Window-wide page shortcut: F3 = search
        /// </summary>
        private void Filter_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            args.Handled = !args.Handled && Filter.FocusState != FocusState.Programmatic && Filter.Focus(FocusState.Programmatic);
        }

        private void SoundLV_SortApply(object sender, RoutedEventArgs e)
        {
            File.ApplySoundOrder();
        }

        private void SoundLV_SortRestore(object sender, RoutedEventArgs e)
        {
            File.FilteredSounds.Clear();
            if (Filter.Text == "")
            { for (int i = 0; i < File.Sounds.Count; i++) { File.FilteredSounds.Add(File.Sounds[i]); } }
            else { Filter.Text = ""; }
        }

        private void SoundLV_Sorting(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem SortItem) { File.SortSounds((string)SortItem.Tag); }
        }

        private void AddSound_Click(object sender, RoutedEventArgs e)
        {
            File.AddSound();
        }

        private async void BrowseSamples_Click(object sender, RoutedEventArgs e)
        {
            // WIP: Add support for multiple files, and folders (with .wav filter?)
            if (await Util.LoadDialogue(".wav", ".xbadpcm", ".vag", ".dsp", ".xma") is Windows.Storage.StorageFile AFile)
            {
                File.AddSampleSound(AFile, File.FilteredSounds.Count);
            }
        }

        private async void ExtractSamples_Click(object sender, RoutedEventArgs e)
        {
            await File.ExtractSamples();
        }

        private async void ExtractSamplesRaw_Click(object sender, RoutedEventArgs e)
        {
            await File.ExtractSamples(true);
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            File.SampleEditorOpen = false;
            Filter.Text = "";
        }

        private void SoundsList_DragItemsStarting(object sender, DragItemsStartingEventArgs e)
        {
            System.Collections.Generic.IList<object> Sounds = e.Items; // must assign to not loose
            e.Data.Properties.Add("SelectedSounds", Sounds);
        }

        private void SoundsList_DragOver(object sender, DragEventArgs e)
        {
            if (e.DataView.Properties.ContainsKey("SelectedSounds")
                || e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                e.AcceptedOperation = DataPackageOperation.Copy;
                e.DragUIOverride.Caption = $"Add sound(s)";
            }
        }

        private async void SoundsList_Drop(object sender, DragEventArgs e)
        {
            var position = e.GetPosition(SoundListView);
            int index = 0; for (; index < SoundListView.Items.Count; index++)
            {
                if (SoundListView.ContainerFromIndex(index) is ListViewItem LVI
                    && position.Y < (double)(LVI.TransformToVisual(SoundListView)
                    .TransformPoint(new(0, 0)).Y + (LVI.ActualHeight - 1)))
                { break; }
            }
            if (e.DataView.Properties["SelectedSounds"] is System.Collections.Generic.IList<object> Sounds)
            {
                for (int i = 0; i < Sounds.Count; i++)
                {
                    UISound S = (UISound)Sounds[i];
                    UISound So = new(S.Hash, S.Flags, S.Sample);
                    File.Samples.Add(S.Sample);
                    File.AddSound(So, index);
                }
            }
            else if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                var Items = await e.DataView.GetStorageItemsAsync();
                for (int i = 0; i < Items.Count; i++)
                {
                    if (Items[i] is Windows.Storage.StorageFile Sample)
                    {
                        File.AddSampleSound(Sample, index);
                    }
                    else if (Items[i] is Windows.Storage.StorageFolder Dir)
                    {
                        foreach (Windows.Storage.StorageFile SubFile in (await Dir.GetFilesAsync()))
                        {
                            // WIP: Only supporting WAV atm
                            if (SubFile.ContentType == "audio/wav") { File.AddSampleSound(SubFile, index); }
                        }
                    }
                }
            }
        }

        private void SoundsList_Delete(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            foreach (Microsoft.UI.Xaml.Data.ItemIndexRange IR in SoundListView.SelectedRanges.Reverse())
            {
                for (int i = IR.LastIndex; i >= IR.FirstIndex; i--) { File.RemoveSoundAt(i); }
            }
            args.Handled = true;
        }

        private async void Sample_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            if (sender is Grid grid && grid.DataContext is UISample Sample)
            {
                try { _ = await TemporaryPlayer.Play(File.ExtractedDir, Sample); } catch { }
            }
            e.Handled = true;
        }

        private void Sound_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            Play_Click(sender, e);
            e.Handled = true;
        }

        private async void Play_Click(object sender, RoutedEventArgs e)
        {
            SoundNotPlayed.IsOpen = false;
            if (File.SelectedSound is UISound Sound)
            {
                try
                {
                    bool Played = await TemporaryPlayer.Play(File.ExtractedDir, Sound.Sample);
                    SoundNotPlayed.IsOpen = !Played;
                }
                catch // (Exception ex) // WIP: Specify message
                {
                    SoundNotPlayed.IsOpen = true;
                }
            }
        }

        private void Play_Sound(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            Play_Click(sender, new());
        }

        private void SampleRateComboBox_DropDownClosed(object sender, object e)
        {
            File.DeleteSelectedSoundSampleFile();
        }

        private void SampleReorderButton_Click(object sender, RoutedEventArgs e)
        {
            File.SampleEditorOpen = true;
            Filter.Text = "";
        }

        private void SampleDropArea_DragEnter(object sender, DragEventArgs e)
        {
            SampleDropArea.Visibility = Visibility.Visible;
        }

        private async void SampleDropAreaBG_DragOver(object sender, DragEventArgs e)
        {
            e.DragUIOverride.IsCaptionVisible = true;
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                e.AcceptedOperation = DataPackageOperation.Copy;
                e.DragUIOverride.Caption = $"Add sound";
                SampleDropArea.Visibility = Visibility.Visible;
            }
            else
            {
                e.AcceptedOperation = DataPackageOperation.None;
            }
        }

        private void SampleDropAreaBG_DragLeave(object sender, DragEventArgs e)
        {
            SampleDropArea.Visibility = Visibility.Collapsed;
        }

        private async void SampleDropAreaBG_Drop(object sender, DragEventArgs e)
        {
            if (File.SelectedSound is UISound Sound
                && e.DataView.Contains(StandardDataFormats.StorageItems)
                && (await e.DataView.GetStorageItemsAsync())[0] is Windows.Storage.StorageFile Sample
                && File.AddSample(Sample) is UISample sample)
            {
                Sound.Sample = sample;
            }
            SampleDropArea.Visibility = Visibility.Collapsed;
        }

        private void SidebarToggle_Click(object sender, RoutedEventArgs e)
        {
            SplitView.IsPaneOpen = !SplitView.IsPaneOpen;
            if (SidebarToggle.Content is FontIcon icon)
            {
                icon.Glyph = SplitView.IsPaneOpen ? "\uE76C" : "\uE76B";
            }
        }

        private void UpdateEvents_Click(object sender, RoutedEventArgs e) { File.HashToEvents(); }

        private void CopyHash_Click(object sender, RoutedEventArgs e)
        {
            if (File.SelectedSound is null) { return; }
            DataPackage dataPackage = new() { RequestedOperation = DataPackageOperation.Copy };
            dataPackage.SetText($"{File.SelectedSound.Hash}".ToLower());
            Clipboard.SetContent(dataPackage);
        }
    }
}
