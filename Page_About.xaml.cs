using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using Zsnd_UI.Functions;

namespace Zsnd_UI
{
#pragma warning disable IDE1006 // Naming Styles
    public class GHAsset
    {
        public int size { get; set; }
        public int download_count { get; set; }
        public string? browser_download_url { get; set; }
    }

    public class GHRelease
    {
        public string? tag_name { get; set; }
        public bool draft { get; set; }
        public bool prerelease { get; set; }
        public string? updated_at { get; set; }
        public System.Collections.Generic.List<GHAsset>? assets { get; set; }
        public string? body { get; set; }
    }
#pragma warning restore IDE1006 // Naming Styles

    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Page_About : Page
    {
        private HttpClient? client;
        private GHRelease? update_info;
        private System.Threading.CancellationTokenSource? cancelts;
        private static readonly Microsoft.Windows.ApplicationModel.Resources.ResourceLoader _resourceLoader = new();

        public Page_About()
        {
            InitializeComponent();
        }

        private async void CheckUpdate_Click(object sender, RoutedEventArgs e)
        {
            UpdateProgress.Visibility = Visibility.Visible;
            UpdateProgress.IsIndeterminate = true;
            client = new();
            HttpRequestMessage requestMessage = new(HttpMethod.Get, "https://api.github.com/repos/ak2yny/Zsnd-UI/releases/latest");
            requestMessage.Headers.Add("User-Agent", "Zsnd-UI");
            HttpResponseMessage response = await client.SendAsync(requestMessage).WaitAsync(TimeSpan.FromMinutes(1));
            UpdateFailedAccess.IsOpen = !response.IsSuccessStatusCode;
            if (response.IsSuccessStatusCode)
            {
                update_info = await response.Content.ReadFromJsonAsync<GHRelease>().WaitAsync(TimeSpan.FromMinutes(1));
                UpdateFailedRetrieve.IsOpen = update_info is null || update_info.assets is null || update_info.assets.Count == 0;
                if (!UpdateFailedRetrieve.IsOpen)
                {
                    UpdateIsCurrent.IsOpen = update_info!.prerelease || update_info.draft || $"v{ViewModels.Shared.Version}" == update_info.tag_name;
                    if (!UpdateIsCurrent.IsOpen)
                    {
                        UpdateInfoTitle.Text = string.Format(_resourceLoader.GetString("UpdateAvailable"), update_info.tag_name, update_info.updated_at);
                        UpdateInfoBody.Text = update_info.body;
                        UpdateInfo.Visibility = Visibility.Visible;
                        UpdateProgress.Visibility = Visibility.Collapsed;
                        return;
                    }
                }
            }
            UpdateProgress.Visibility = Visibility.Collapsed;
            client.Dispose(); client = null;
        }

        private async void Update_Click(object sender, RoutedEventArgs e)
        {
            if (client is null || update_info is null) { return; }
            string architectureNexe = $"{System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture}.exe";
            int i = 0; while (i < update_info.assets!.Count) { if (update_info.assets[i++].browser_download_url!.EndsWith(architectureNexe, StringComparison.OrdinalIgnoreCase)) { break; } }
            GHAsset asset = update_info.assets[i];
            if (asset.browser_download_url is not string InstallerUrl) { return; }
            cancelts?.Dispose(); cancelts = new();
            UpdateProgress.IsIndeterminate = false;
            UpdateProgress.Value = 0;
            UpdateProgress.Visibility = CancelButton.Visibility = Visibility.Visible;
            UpdateButton.Visibility = Visibility.Collapsed;
            double factor = 95.0 / asset.size;
            IProgress<double> progress = new Progress<double>(value => UpdateProgress.Value = 5.0 + value * factor);
            string? InstallerName = Path.GetFileName(InstallerUrl);
            string Installer = Path.Combine(ZsndPath.CD, InstallerName);
            string InstallBat = Path.Combine(ZsndPath.CD, $"{Path.GetFileNameWithoutExtension(InstallerName)}.bat");
            try
            {
                using Stream s = await client.GetStreamAsync(InstallerUrl).WaitAsync(TimeSpan.FromMinutes(1));
                using FileStream fs = new(Installer, FileMode.Create);
                await s.CopyToWithProgressAsync(fs, asset.size, progress, cancelts.Token);
                fs.Close();
                ContentDialog dialog = new()
                {
                    Title = _resourceLoader.GetString("UpdateInstallTitle"),
                    Content = string.Format(_resourceLoader.GetString("UpdateInstallContent"), Installer),
                    PrimaryButtonText = _resourceLoader.GetString("UpdateInstallPrimaryButton"),
                    CloseButtonText = _resourceLoader.GetString("UpdateInstallCloseButton"),
                    XamlRoot = XamlRoot
                };
                ContentDialogResult result = await dialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    File.WriteAllText(InstallBat,
                        $"cd \\d \"{ZsndPath.CD}\"\n\"{Installer}\" -y -InstallPath=\"{ZsndPath.CD}\" && del {InstallerName}\nstart Zsnd-UI.exe\ndel {InstallBat}");
                    _ = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(InstallBat) { CreateNoWindow = true });
                    Application.Current.Exit();
                    client.Dispose(); cancelts.Dispose();
                    return;
                }
            }
            catch (OperationCanceledException) { try { File.Delete(Installer); } catch { } }
            catch (Exception ex) { UpdateFailed.Message = ex.Message; UpdateFailed.IsOpen = true; }
            UpdateButton.Visibility = Visibility.Visible;
            UpdateInfo.Visibility = CancelButton.Visibility = Visibility.Collapsed;
            client.Dispose(); cancelts.Dispose();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            cancelts?.Cancel();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            App.MWindow?.TryGoBack();
        }
    }
}
