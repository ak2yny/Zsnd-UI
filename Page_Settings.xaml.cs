using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using Zsnd_UI.ViewModels;

namespace Zsnd_UI
{
    /// <summary>
    /// The settings page with settngs cards. WIP
    /// </summary>
    public sealed partial class Page_Settings : Page
    {
        internal ZsndUISettings Cfg { get; } = Shared.UI;

        public Page_Settings()
        {
            InitializeComponent();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e) { App.MWindow?.TryGoBack(); }

        private async void ExtractedAssetsPath_Click(object sender, RoutedEventArgs e)
        {
            Cfg.ExtractedAssetsPath = await BrowseFolder();
        }
        /// <summary>
        /// Open folder dialogue.
        /// </summary>
        public static async Task<string> BrowseFolder()
        {
            FolderPicker folderPicker = new();
            folderPicker.FileTypeFilter.Add("*");
            WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, WinRT.Interop.WindowNative.GetWindowHandle(App.MWindow));
            StorageFolder folder = await folderPicker.PickSingleFolderAsync();
            return folder is null ? "" : folder.Path;
        }
    }
}
