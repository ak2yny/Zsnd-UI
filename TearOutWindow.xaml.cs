using Microsoft.UI.Xaml;

namespace Zsnd_UI
{
    /// <summary>
    /// A sentinel tear out window with a more compact title area, to be closed separately (tracked).
    /// </summary>
    public sealed partial class TearOutWindow : Window
    {
        public TearOutWindow()
        {
            //Activated += Window_Activated;
            InitializeComponent();
            ExtendsContentIntoTitleBar = true;
            SetTitleBar(TabViewPage.TitleBarDragRegion);
            AppWindow.TitleBar.ButtonForegroundColor = Microsoft.UI.Colors.AliceBlue;
            TabViewPage.VM.WindowID = AppWindow.Id;
            TabViewPage.VM.IsMainTabView = false;
        }
        /// <summary>
        /// Make the title dim when the app is not in focus
        /// </summary>
        //private void Window_Activated(object sender, WindowActivatedEventArgs args)
        //{
        //    AppTitleIcon.Opacity =
        //        args.WindowActivationState == WindowActivationState.Deactivated ? 0.5 : 1.0;
        //}
        /// <summary>
        /// Add the specified <paramref name="tab"/> to the embedded <see cref="Page_TabView"/>.
        /// </summary>
        internal void AddTab(lib.ZsndFile tab)
        {
            TabViewPage.AddTab(tab);
        }
    }
}
