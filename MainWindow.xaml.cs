using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Zsnd_UI.ViewModels;

namespace Zsnd_UI
{
    /// <summary>
    /// Main Window
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        internal ZsndUISettings Cfg { get; } = Shared.UI;

        public MainWindow()
        {
            Activated += MainWindow_Activated;
            Closed += static (sender, args) => App.Close();
            InitializeComponent();
            ExtendsContentIntoTitleBar = true;
            SetTitleBar(AppTitleBar);
            AppWindow.TitleBar.ButtonForegroundColor = Microsoft.UI.Colors.AliceBlue;
            _ = FrameLeft.Navigate(typeof(Page_TabView));
            Cfg.PropertyChanged += (sender, args) => { if (args.PropertyName == nameof(ZsndUISettings.SplitView)) { FrameRight_VisibilityChanged(); } };
        }

        public void NavigateToPage(System.Type PageType)
        {
            _ = FrameLeft.Navigate(PageType);
        }

        private void FrameRight_Loaded(object sender, RoutedEventArgs e)
        {
            FrameRight_VisibilityChanged();
        }

        public void TryGoBack()
        {
            if (FrameLeft.CanGoBack) { FrameLeft.GoBack(); /*return true;*/}
            // return false;
        }
        /// <summary>
        /// Make the title dim when the app is not in focus
        /// </summary>
        private void MainWindow_Activated(object sender, WindowActivatedEventArgs args)
        {
            AppTitleBar.Opacity = args.WindowActivationState == WindowActivationState.Deactivated ? 0.5 : 1.0;
        }

        private void FrameRight_VisibilityChanged()
        {
            if (Cfg.SplitView && FrameRight.Content is null)
            {
                _ = FrameRight.Navigate(typeof(Page_TabView));
                ((Page_TabView)FrameRight.Content!).VM.IsMainTabView = false;
            }
        }
    }
}
