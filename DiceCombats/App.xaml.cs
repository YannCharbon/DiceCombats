using Microsoft.Maui.Controls.PlatformConfiguration.AndroidSpecific;
using System.Diagnostics;
using System.Globalization;
using Application = Microsoft.Maui.Controls.Application;

namespace DiceCombats
{
    public partial class App : Application
    {
        public App()
        {
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

            Preferences.Set("Language", cultureCode);

            ReloadPages();
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
