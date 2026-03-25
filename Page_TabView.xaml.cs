using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.ApplicationModel.DataTransfer;
using Zsnd_UI.Functions;
using Zsnd_UI.lib;
using Zsnd_UI.ViewModels;

namespace Zsnd_UI
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Page_TabView : Page
    {
        internal TabViewModel VM { get; } = new();
        internal ZsndUISettings Cfg { get; } = Shared.UI;
        // WIP: We might want to share the closed tabs across windows
        private System.Collections.Generic.List<ZsndFile> ClosedTabs { get; } = [];

        public Grid TitleBarDragRegion => DragRegion;

        private DispatcherTimer? _hoverTimer;

        public Page_TabView()
        {
            InitializeComponent();
        }

        internal void AddTab(ZsndFile file)
        {
            // ~ 5 - 10 mb for empty tab; + ~10mb for 100 sounds. Too memory hungry?
            VM.Tabs.Add(file);
            MainTabView.SelectedIndex = VM.Tabs.Count - 1;
        }

        private void AddClosedTab(ZsndFile tab)
        {
            if (ClosedTabs.Count == 20) { ClosedTabs.RemoveAt(19); RecentlyClosed.Items.RemoveAt(20); }
            if (ClosedTabs.Count == 0)
            {
                NoRecentlyClosed.Visibility = Visibility.Collapsed;
                LastRecentlyClosed.Visibility = Visibility.Visible;
            }
            else
            {
                MenuFlyoutItem ct = new() { Text = LastRecentlyClosed.Text };
                ct.Click += ReOpenTab_Click;
                RecentlyClosed.Items.Insert(2, ct);
            }
            LastRecentlyClosed.Text = tab.Name;
            ClosedTabs.Insert(0, tab);
        }

        internal void RemoveTab(ZsndFile tab, bool track = true)
        {
            // If tracking, change to do so globally? (see above)
            tab.CancelOperations();
            if (VM.Tabs.Remove(tab)) { if (VM.Tabs.Count == 0) { App.CloseWindow(VM.WindowID); } else if (track) { AddClosedTab(tab); } }
        }

        private void MainTabView_TabCloseRequested(TabView sender, TabViewTabCloseRequestedEventArgs args)
        {
            if (args.Item is ZsndFile tab) { RemoveTab(tab); }
        }

        private void MainTabView_AddTabButtonClick(TabView sender, object args) => AddTab(new());

        private void MainTabView_TabDragStarting(TabView sender, TabViewTabDragStartingEventArgs args)
        {
            args.Data.Properties.Add("Tab", args.Item);
            args.Data.Properties.Add("SourcePage", this);
        }

        private void MainTabView_TabStripDragOver(object sender, DragEventArgs args)
        {
            if (args.DataView.Properties.ContainsKey("Tab"))
            {
                args.AcceptedOperation = DataPackageOperation.Move;
            }
        }

        private void MainTabView_TabDroppedOutside(TabView sender, TabViewTabDroppedOutsideEventArgs args)
        {
            if (args.Item is ZsndFile tab)
            {
                TearOutWindow w = App.CreateWindow();
                w.AddTab(tab);
                RemoveTab(tab, false);
                w.Activate();
            }
        }

        private void MainTabView_TabStripDrop(object sender, DragEventArgs args)
        {
            if (args.DataView.Properties.TryGetValue("Tab", out object t) && t is ZsndFile tab &&
                args.DataView.Properties["SourcePage"] is Page_TabView SourceTVP
                && sender is TabView TV)
            {
                // Note: No need to check if the source page is the same as the target page, reorder takes care of that.
                Windows.Foundation.Point Pos = args.GetPosition(TV);
                int i = 0; for (; i < VM.Tabs.Count; i++)
                {
                    if (TV.ContainerFromIndex(i) is UIElement tabContainer)
                    {
                        if (Pos.X < tabContainer.TransformToVisual(TV)
                            .TransformPoint(new Windows.Foundation.Point()).X +
                            ((double)tabContainer.ActualSize.X / 2))
                        {
                            break;
                        }
                    }
                }
                SourceTVP.RemoveTab(tab, false);
                VM.Tabs.Insert(i, tab);
                TV.SelectedIndex = i;
            }

        }
        /* Tear out variant (no support for move and rearrange; but supports direct snapping)
        private Window? _tearOut;

        private void MainTabView_ExternalTornOutTabsDropped(TabView sender, TabViewExternalTornOutTabsDroppedEventArgs args)
        {
            if (args.Tabs[0] is TabViewItem TVI && TVI.Content is Tab_SoundsView SV && SV.DataContext is ZsndFile tab
                && UIHelpers.FindFirstParent<TabView>(TVI) is TabView TV && TV != sender && TV.TabItems.Remove(tab))
            {
                sender.TabItems.Insert(args.DropIndex, tab);
            }
        }

        private void MainTabView_ExternalTornOutTabsDropping(TabView sender, TabViewExternalTornOutTabsDroppingEventArgs args)
        {
            args.AllowDrop = args.Tabs.Length > 1;
        }

        private void MainTabView_TabTearOutRequested(TabView sender, TabViewTabTearOutRequestedEventArgs args)
        {
            if (_tearOut?.Content is Page_TabView ptv
                && args.Tabs[0] is TabViewItem TVI
                && TVI.Content is Tab_SoundsView SV
                && SV.DataContext is ZsndFile tab)
            {
                _ = ((System.Collections.ObjectModel.ObservableCollection<ZsndFile>)sender.TabItemsSource).Remove(tab); // not added to recently closed
                //NewWindowTabs.Add(tab);
                ptv.Tabs.Add(tab);
            }
        }

        private void MainTabView_TabTearOutWindowRequested(TabView sender, TabViewTabTearOutWindowRequestedEventArgs args)
        {
            //if (args.Items[0] is ZsndFile item)
            _tearOut = App.CreateWindow();
            _tearOut.Content = new Page_TabView();
            args.NewWindowId = _tearOut.AppWindow.Id;
            //NewWindowTabs = w.Tabs;
        }
        */

        private void MainTabView_DragEnter(object sender, DragEventArgs e)
        {
            if (e.DataView.Properties.ContainsKey("SelectedSounds")
                && sender is DependencyObject d
                && Util.FindFirstParent<TabViewItem>(d) is TabViewItem TVI
                && TVI.Content is Controls.Tab_SoundsView tab)
            {
                e.AcceptedOperation = DataPackageOperation.Copy;
                _hoverTimer?.Stop();
                _hoverTimer = new DispatcherTimer { Interval = System.TimeSpan.FromMilliseconds(400) };
                _hoverTimer.Tick += (s, args) =>
                {
                    _hoverTimer.Stop();
                    MainTabView.SelectedItem = tab.DataContext;
                };
                _hoverTimer.Start();
            }
        }

        private void NewMenuItem_Click(object sender, RoutedEventArgs e) => AddTab(new());

        private async void OpenMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (await Util.LoadDialogue(".zsm", ".zss", ".enm", ".ens", ".json") is Windows.Storage.StorageFile ZFile)
            {
                if (MainTabView.SelectedItem is ZsndFile tab
                    && tab.ZsndFilePath == "" && tab.Sounds.Count == 0)
                {
                    tab.LoadFile(ZFile.Path, ZFile.FileType, ZFile.DisplayName);
                }
                else { AddTab(new(ZFile.Path, ZFile.FileType, ZFile.DisplayName)); }
            }
        }

        private void SaveMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (MainTabView.SelectedItem is ZsndFile tab) { tab.SaveJson(); }
        }

        private void SaveAllMenuItem_Click(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < VM.Tabs.Count; i++) { VM.Tabs[i].SaveJson(); }
        }

        private async void SaveAsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (MainTabView.SelectedItem is ZsndFile tab && !tab.TaskInfo.IsRunning
                && await Util.SaveDialogue() is Windows.Storage.StorageFile ZFile)
            {
                tab.SaveFile(ZFile.Path, ZFile.FileType, ZFile.DisplayName);
            }
        }

        private async void SaveZsndMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (MainTabView.SelectedItem is ZsndFile tab) { tab.SaveZsnd(); }
        }

        private void CloseMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (MainTabView.SelectedItem is ZsndFile tab) { RemoveTab(tab); }
        }

        private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            // WIP: Add all tabs to closed?
            if (VM.IsMainTabView) { App.Close(); } else { App.CloseWindow(VM.WindowID); } // reflecting Alt+F4 behaviour
        }

        private void ReOpenTab_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem ct)
            {
                int i = RecentlyClosed.Items.IndexOf(ct);
                VM.Tabs.Add(ClosedTabs[i - 1]);
                ClosedTabs.RemoveAt(i - 1);
                if (ClosedTabs.Count == 0)
                {
                    NoRecentlyClosed.Visibility = Visibility.Visible;
                    LastRecentlyClosed.Visibility = Visibility.Collapsed;
                }
                else if (i == 1) // 0 should never happen, so otherwise it's 2+
                {
                    LastRecentlyClosed.Text = ((MenuFlyoutItem)RecentlyClosed.Items[2]).Text;
                    RecentlyClosed.Items.RemoveAt(2);
                }
                else { RecentlyClosed.Items.RemoveAt(i); }
            }
        }

        private void MenuView_CM_Click(object sender, RoutedEventArgs e)
        {
            VM.CompactMode = !VM.CompactMode;
        }

        private void AboutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            App.MWindow?.NavigateToPage(typeof(Page_About));
        }

        private void NavigateToTabNrKB_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            int i = (int)sender.Key - 49;
            MainTabView.SelectedIndex = i > -1 && i < VM.Tabs.Count ? i : VM.Tabs.Count - 1;
            args.Handled = true;
        }
        /// <summary>
        /// Handles the completion event of the gear rotation animation and initiates the closing storyboard sequence.
        /// </summary>
        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            //if (SettingsButton.Resources["GearRotateOpen"] is Microsoft.UI.Xaml.Media.Animation.Storyboard sb) { sb?.Begin(); }
            App.MWindow?.NavigateToPage(typeof(Page_Settings));
        }
        /// <summary>
        /// Unused: Handles the completion event of the gear rotation animation and initiates the closing storyboard sequence.
        /// </summary>
        private void GearRotate_Completed(object sender, object e)
        {
            if (SettingsButton.Resources["GearRotateClose"] is Microsoft.UI.Xaml.Media.Animation.Storyboard sb) { sb?.Begin(); }
        }
    }
}
