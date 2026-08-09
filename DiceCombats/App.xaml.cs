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
            return new Microsoft.Maui.Controls.Window(new MainPage());
        }

        public void SetCultureAndReload(string cultureCode)
        {
            var culture = new CultureInfo(cultureCode);
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;

            Preferences.Set("Language", cultureCode);

            // Reload the Blazor-based UI on the single window this app has.
            if (Windows.Count > 0)
            {
                Windows[0].Page = new MainPage();
            }
        }
    }
}
