using Microsoft.Maui.Controls.PlatformConfiguration.AndroidSpecific;
using System.Diagnostics;
using System.Globalization;
using DiceCombats.Storage;
using Application = Microsoft.Maui.Controls.Application;

namespace DiceCombats
{
    public partial class App : Application
    {
        private readonly ILocalPreferences _localPreferences;
        private readonly AppSettings _settings;
        private readonly DataLocation _location;

        public App(ILocalPreferences localPreferences, AppSettings settings, DataLocation location)
        {
            _localPreferences = localPreferences;
            _settings = settings;
            _location = location;

            InitializeComponent();

            var currentApp = Application.Current;
            if (currentApp != null)
            {
                currentApp.On<Microsoft.Maui.Controls.PlatformConfiguration.Android>().UseWindowSoftInputModeAdjust(WindowSoftInputModeAdjust.Resize);
            }
        }

        // Replaces the deprecated MainPage assignment: MAUI builds the root page
        // through the window now.
        protected override Microsoft.Maui.Controls.Window CreateWindow(IActivationState? activationState)
        {
            // The caption is hidden because the web page draws the title bar,
            // but Windows still reads the window Title for the taskbar tooltip,
            // Alt+Tab and the window list. Without it the app showed its icon
            // there with no name beside it.
            return new Microsoft.Maui.Controls.Window(new MainPage()) { Title = "DiceCombats" };
        }

        public void SetCultureAndReload(string cultureCode)
        {
            var culture = new CultureInfo(cultureCode);
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;

            // The language travels in settings.json; a copy stays on this
            // computer, so that the startup screen speaks it when the data
            // folder cannot be reached.
            if (_settings.IsInitialized)
            {
                _settings.Set("Language", cultureCode);
            }
            _localPreferences.Set("Language", cultureCode);

            ReloadPages();
        }

        /// <summary>
        /// Starts the app again, after the data folder changed. In a debug run on
        /// a sandbox (DICE_DATA_DIR) it only quits: a restarted process might not
        /// get that variable, and would then open the real data.
        /// </summary>
        public void Restart()
        {
#if WINDOWS
            if (!_location.IsDebugOverride)
            {
                var failure = Microsoft.Windows.AppLifecycle.AppInstance.Restart(string.Empty);
                Debug.WriteLine($"Restart failed: {failure}");
            }
#endif
            Quit();
        }

        /// <summary>
        /// Raised when the pages must be built again. The main layout answers by
        /// reloading the web page in place.
        /// </summary>
        public static event Action? PagesReloadRequested;

        /// <summary>
        /// Builds every page again, so that they pick up a new language or data
        /// that was replaced.
        ///
        /// On Windows the web page is reloaded inside the same BlazorWebView:
        /// replacing the MAUI page creates a new WebView2 under a new navigation
        /// view, and WebTitleBar only lifts the first one into the title-bar
        /// strip, so the new one sat 32 px lower, under a dark band. Replacing
        /// the page is kept for the other platforms, and for the case where no
        /// layout is there to answer.
        /// </summary>
        public void ReloadPages()
        {
            var reloadInPlace = PagesReloadRequested;
#if WINDOWS
            if (reloadInPlace != null)
            {
                reloadInPlace();
                return;
            }
#endif
            if (Windows.Count > 0)
            {
                Windows[0].Page = new MainPage();
            }
        }
    }
}
