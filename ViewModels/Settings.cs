using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using System.IO;
using System.Xml.Serialization;

namespace Zsnd_UI.ViewModels
{
    /// <summary>
    /// XML serializable UI settings. XML attributes are public properties, other fields are not serialized.
    /// </summary>
    public partial class ZsndUISettings : ObservableObject
    {
        //[ObservableProperty]
        //public partial string? GameOrModPath { get; set; }
        [ObservableProperty]
        internal partial string? LastOpenedFilePath { get; set; }
        [ObservableProperty]
        public partial string ExtractedAssetsPath { get; set; } = Path.GetTempPath(); // "D:\\GitHub\\Zsnd-UI\\Assets\\extractedSoundsTemp"
        [ObservableProperty]
        public partial bool BigBanner { get; set; } = true;
        [ObservableProperty]
        public partial bool SplitView { get; set; }
        // Don't save:
        [ObservableProperty]
        internal partial GridLength SplitViewRightPanelWidth { get; set; } = new(0);
        [ObservableProperty]
        internal partial Thickness TabViewTopMargin { get; private set; } = new(0, 60, 0, 0);
        [ObservableProperty]
        internal partial VerticalAlignment BannerVAlignment { get; private set; } = VerticalAlignment.Center;
        [ObservableProperty]
        internal partial GridLength BannerHeight { get; private set; } = new(100);

        partial void OnBigBannerChanged(bool value)
        {
            TabViewTopMargin = new Thickness(0, value ? 60 : 0, 0, 0);
            BannerVAlignment = value ? VerticalAlignment.Center : VerticalAlignment.Bottom;
            BannerHeight = new GridLength(value ? 100 : 120);
        }

        partial void OnSplitViewChanged(bool value)
        {
            SplitViewRightPanelWidth = value ? new GridLength(1, GridUnitType.Star) : new(0);
        }
    }
    /// <summary>
    /// The main static configuration class. 
    /// </summary>
    internal static class Shared
    {
        internal static ZsndUISettings UI { get; set; }

        internal const string GitHub = "https://github.com/ak2yny/Zsnd-UI";
        internal static readonly string? Version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString();

        private static readonly string config = Path.Combine(Functions.ZsndPath.CD, "config.xml");

        static Shared()
        {
            try
            {
                XmlSerializer XS = new(typeof(ZsndUISettings));
                using FileStream fs = new(config, FileMode.Open, FileAccess.Read);
                UI = XS.Deserialize(fs) is ZsndUISettings Cfg ? Cfg : new();
            }
            catch { UI = new(); }
        }

        internal static void SaveConfig()
        {
            try
            {
                XmlSerializer XS = new(typeof(ZsndUISettings));
                using FileStream fs = File.Open(config, FileMode.Create);
                XS.Serialize(fs, UI);
            }
            catch
            {
                // WIP?
            }
        }
    }
}
