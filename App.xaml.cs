using Microsoft.UI;
using Microsoft.UI.Xaml;
using System.IO;

namespace Zsnd_UI
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            InitializeComponent();
            UnhandledException += App_UnhandledException;
        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            MWindow = new MainWindow();
            MWindow.Activate();
        }

        private void App_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            try
            {
                using StreamWriter sw = File.AppendText(Path.Combine(Functions.ZsndPath.CD, "error.log"));
                sw.Write($@"
====================================================
Logged  {System.DateTime.Now}
Version {ViewModels.Shared.Version}
----------------------------------------------------
{e.Exception /*Provides details, less details: e.Message*/}
"); // more details: e.Exception.StackTrace + e.Exception.InnerException
                sw.Close();
            }
            catch { }
            e.Handled = true;
            Close();
            MWindow?.Close();
        }

        public static TearOutWindow CreateWindow()
        {
            TearOutWindow window = new();
            WindowId ID = window.AppWindow.Id;
            window.Closed += (sender, args) =>
            {
                _ = ActiveWindows.Remove(ID);
            };
            ActiveWindows[ID] = window;
            return window;
        }

        public static void CloseWindow(WindowId ID)
        {
            if (ActiveWindows.TryGetValue(ID, out Window? w)) { w.Close(); }
        }

        public static void Close()
        {
            foreach (Window w in ActiveWindows.Values) { w.Close(); }
            ViewModels.Shared.SaveConfig();
        }

        public static MainWindow? MWindow { get; set; }

        public static System.Collections.Generic.Dictionary<WindowId, Window> ActiveWindows { get; } = [];
    }
}
