using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using Zsnd_UI.lib;

namespace Zsnd_UI.ViewModels
{
    internal partial class TabViewModel : ObservableObject
    {
        [ObservableProperty]
        internal partial bool CompactMode { get; set; } = !Shared.UI.BigBanner;

        [ObservableProperty]
        internal partial bool NonCompactMode { get; private set; } = Shared.UI.BigBanner;

        [ObservableProperty]
        internal partial bool IsMainTabView { get; set; } = true;

        [ObservableProperty]
        internal partial double FooterMinWidth { get; private set; } = Shared.UI.BigBanner ? 0 : 136;

        [ObservableProperty]
        internal partial Microsoft.UI.Xaml.Thickness MenuLeftMargin { get; private set; } = new(Shared.UI.BigBanner ? 0 : 40, 0, 0, 0);

        internal ObservableCollection<ZsndFile> Tabs { get; } = [];

        internal Microsoft.UI.WindowId WindowID { get; set; }

        partial void OnIsMainTabViewChanged(bool value) { CompactMode = !value; }

        partial void OnCompactModeChanged(bool value)
        {
            NonCompactMode = !value;
            FooterMinWidth = NonCompactMode ? 0 : IsMainTabView ? 136 : 170;
            MenuLeftMargin = new(CompactMode ? 40 : 0, 0, 0, 0);
        }
    }
}
