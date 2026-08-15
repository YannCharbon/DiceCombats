/*
 * DiceCombats - Copyright (C) 2025 Yann Charbon
 * SPDX-License-Identifier: GPL-3.0-or-later
 *
 * This file is part of DiceCombats, released under the GNU GPL v3.
 * See the LICENSE file in the repository root for details.
 */

using Microsoft.Extensions.Logging;
using Microsoft.Maui.LifecycleEvents;
using MudBlazor.Services;
using System.Globalization;
using System.Diagnostics;

using DiceCombats.Rendering;                   // <— ADD
using DiceCombats.Rendering.Contracts;        // <— ADD
using DiceCombats.Rendering.Renderers;        // <— ADD
using DiceCombats.Automations;
using DiceCombats.Integrations;

#if WINDOWS
using Microsoft.UI;
using Microsoft.UI.Windowing;
using WinRT.Interop;
using System.Runtime.InteropServices;
#endif

namespace DiceCombats
{
    public static class MauiProgram
    {
        public static IServiceProvider ServiceProvider { get; private set; } = default!;

        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                })
                .ConfigureLifecycleEvents(events =>
                {
#if WINDOWS
                    events.AddWindows(w =>
                    {
                        w.OnWindowCreated(window =>
                        {
                            // Extend content into the title bar at the AppWindow
                            // (OS) level, NOT via the XAML Window property: the
                            // XAML property activates MAUI's own title-bar strip
                            // machinery, which re-inserts a native strip above
                            // the WebView2 on every interactive state change
                            // (restore, drag-resize) and fights any suppression.
                            // AppWindow-level extension bypasses MAUI entirely:
                            // the webview owns the full client area, the system
                            // draws the caption buttons above it and provides
                            // the default drag region across the top strip.
                            window.ExtendsContentIntoTitleBar = false;
                            IntPtr hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
                            WindowId myWndId = Win32Interop.GetWindowIdFromWindow(hWnd);
                            var _appWindow = AppWindow.GetFromWindowId(myWndId);
                            _appWindow.SetPresenter(AppWindowPresenterKind.Overlapped);

                            var titleBar = _appWindow.TitleBar;
                            titleBar.ExtendsContentIntoTitleBar = true;
                            titleBar.ButtonBackgroundColor = Microsoft.UI.Colors.Transparent;
                            titleBar.ButtonInactiveBackgroundColor = Microsoft.UI.Colors.Transparent;
                            titleBar.ButtonForegroundColor = Microsoft.UI.Colors.White;
                            titleBar.ButtonInactiveForegroundColor = Windows.UI.Color.FromArgb(160, 255, 255, 255);
                            titleBar.ButtonHoverBackgroundColor = Windows.UI.Color.FromArgb(32, 255, 255, 255);
                            titleBar.ButtonHoverForegroundColor = Microsoft.UI.Colors.White;

                            // Maximize the window on creation
                            if (_appWindow.Presenter is OverlappedPresenter p)
                            {
                                p.Maximize();
                            }

                            // The web page IS the title bar (Teams/Outlook style):
                            // the WebView2 extends to the window top, the web app
                            // bar carries the gem, and CSS app-region regions
                            // provide dragging.
                            WebTitleBar.Attach(window);

                            // Attach to the Closing event and use the service provider
                            _appWindow.Closing += (s, e) =>
                            {
                                ServiceProvider.GetRequiredService<CombatLogService>().EndSession();

                                var diceService = ServiceProvider.GetRequiredService<DiceCombatsService>();
                                if (diceService != null)
                                {
                                    diceService.SaveCreatures();
                                    diceService.SaveCombats();
                                    _ = diceService.SaveFavoriteCombatsListAsync();
                                    _ = diceService.SaveUserCreatureCustomFieldsListAsync();
                                }
                            };
                        });
                    });
#endif

#if ANDROID
                    events.AddAndroid(android =>
                    {
                        android.OnPause(activity =>
                        {
                            ServiceProvider.GetRequiredService<CombatLogService>().EndSession();

                            // Use the root service provider
                            var diceService = ServiceProvider.GetRequiredService<DiceCombatsService>();
                            if (diceService != null)
                            {
                                diceService.SaveCreatures();
                                diceService.SaveCombats();
                                _ = diceService.SaveFavoriteCombatsListAsync();
                                _ = diceService.SaveUserCreatureCustomFieldsListAsync();
                            }
                        });
                    });
#endif
                });

            builder.Services.AddMauiBlazorWebView();

#if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();
            builder.Logging.AddDebug();
#endif

            builder.Services.AddMudServices();
            builder.Services.AddSingleton<DiceCombatsService>();
            builder.Services.AddHttpClient();
            builder.Services.AddSingleton<IAutomationStorage, MauiAutomationStorage>();
            builder.Services.AddSingleton<AutomationConfigurationService>();
            builder.Services.AddSingleton<EventTemplateRenderer>();
            builder.Services.AddSingleton<EventConditionEvaluator>();
            // The dispatcher is resolved by its own type as well, so the combat
            // log can subscribe to the same instance the publisher writes to.
            builder.Services.AddSingleton<DiceEventDispatcher>();
            builder.Services.AddSingleton<IDiceEventPublisher>(sp => sp.GetRequiredService<DiceEventDispatcher>());
            builder.Services.AddSingleton<CombatLogService>();

#if ANDROID
            builder.Services.AddSingleton<IFileHandler, FileHandler>();
#elif WINDOWS
            builder.Services.AddSingleton<IFileHandler, FileHandler>();
#endif

            builder.Services.AddScoped<PopupNotificationService>();

            builder.Services.AddLocalization();

            // ==================== RENDERING DI ====================
            // Register all renderers and the registry (constructor-injected)
            builder.Services.AddSingleton<IRenderer, TextRenderer>();
            builder.Services.AddSingleton<IRenderer, MarkdownRenderer>();
            builder.Services.AddSingleton<IRenderer, HtmlTemplateRenderer>();
            builder.Services.AddSingleton<IRenderer, PdfRenderer>();
            builder.Services.AddSingleton<RendererRegistry>();
            // ======================================================

            string cultureCode = Preferences.Get("Language", CultureInfo.CurrentUICulture.Name);
            var culture = new CultureInfo(cultureCode);
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;

            var app = builder.Build();
            ServiceProvider = app.Services; // Assign the root service provider

            // Resolved right away: it subscribes to the event stream in its
            // constructor, and a lazily created one would miss everything until
            // something else happened to ask for it.
            app.Services.GetRequiredService<CombatLogService>();

            return app;
        }
    }
}
