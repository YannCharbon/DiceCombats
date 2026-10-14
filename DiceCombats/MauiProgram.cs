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
using DiceCombats.Storage;

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
                            _appWindow.Closing += (s, e) => SaveAndFlush(TimeSpan.FromSeconds(10));
                        });
                    });
#endif

#if ANDROID
                    events.AddAndroid(android =>
                    {
                        // Android may end the process at any time once the app is
                        // in the background. OnPause comes first and must stay
                        // quick, so it only starts the saves; OnStop then waits
                        // for them, briefly.
                        android.OnPause(activity => SaveAndFlush(TimeSpan.Zero));
                        android.OnStop(activity => SaveAndFlush(TimeSpan.FromSeconds(2)));
                    });
#endif
                });

            builder.Services.AddMauiBlazorWebView();

#if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();
            builder.Logging.AddDebug();
#endif

            // Where the data is, and the store every data file goes through.
            // Built first: the language below is read from the local
            // preferences, and in a debug run on a sandbox (DICE_DATA_DIR)
            // those are a file of the sandbox, never the real ones.
#if DEBUG
            string? debugOverride = Environment.GetEnvironmentVariable(DataLocation.DebugOverrideVariable);
#else
            string? debugOverride = null;
#endif
            if (string.IsNullOrWhiteSpace(debugOverride))
            {
                debugOverride = null;
            }
            var localPreferences = LocalPreferences.Create(debugOverride, () => new MauiLocalPreferences());
            var dataLocation = new DataLocation("DiceCombats", debugOverride ?? FileSystem.AppDataDirectory,
                                                localPreferences, GetMachineName(), debugOverride);
            Debug.WriteLine($"Data folder: {dataLocation.Root} ({dataLocation.State}{(dataLocation.IsDebugOverride ? ", " + DataLocation.DebugOverrideVariable : "")})");
#if DEBUG
            // Until the startup screen exists, a folder that cannot be used
            // stops a debug run here rather than showing an empty library.
            if (dataLocation.State != DataLocationState.Ok)
            {
                throw new InvalidOperationException(
                    $"The data folder {dataLocation.Root} cannot be used ({dataLocation.State}). Check {DataLocation.DebugOverrideVariable}.");
            }
#endif
            builder.Services.AddSingleton<ILocalPreferences>(localPreferences);
            builder.Services.AddSingleton(dataLocation);
            builder.Services.AddSingleton(new SafeFileStore(dataLocation.GetPath));
            builder.Services.AddSingleton<StorageNotices>();

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
            builder.Services.AddSingleton<BackupService>();
            builder.Services.AddSingleton<DiceCombats.Portability.DCBundleService>();

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

            string cultureCode = localPreferences.Get("Language", CultureInfo.CurrentUICulture.Name);
            var culture = new CultureInfo(cultureCode);
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;

            var app = builder.Build();
            ServiceProvider = app.Services; // Assign the root service provider

            // Resolved right away: it subscribes to the event stream in its
            // constructor, and a lazily created one would miss everything until
            // something else happened to ask for it.
            app.Services.GetRequiredService<CombatLogService>();

            // Every data file is read before the first page can ask for it, or
            // save over it. Until the startup screen exists, this waits here.
            var diceService = app.Services.GetRequiredService<DiceCombatsService>();
            var automationConfig = app.Services.GetRequiredService<AutomationConfigurationService>();
            Task.Run(async () =>
            {
                await diceService.InitializeAsync();
                await automationConfig.InitializeAsync();
            }).GetAwaiter().GetResult();
            app.Services.GetRequiredService<StorageNotices>().ReportLoadFailures();

            // The first start of the day on this machine makes a backup, in the
            // background. Not after a file failed to load: the backups kept
            // would slowly be replaced by copies of the broken state.
            if (app.Services.GetRequiredService<SafeFileStore>().BlockedFiles.Count == 0)
            {
                var backups = app.Services.GetRequiredService<BackupService>();
                var store = app.Services.GetRequiredService<SafeFileStore>();
                _ = Task.Run(async () =>
                {
                    try
                    {
                        // After the saves the load itself started (the pinned
                        // list migration) are on disk.
                        await store.FlushAsync();
                        await backups.AutoBackupIfDueAsync();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Automatic backup failed: {ex}");
                    }
                });
            }

            return app;
        }

        private static string GetMachineName()
        {
#if ANDROID
            return DeviceInfo.Current.Name;
#else
            return Environment.MachineName;
#endif
        }

        /// <summary>
        /// Saves what is in memory, then waits at most <paramref name="wait"/>
        /// until every write has reached the disk (no wait for TimeSpan.Zero).
        /// Called when the window closes (Windows) and when the app goes to the
        /// background (Android).
        /// </summary>
        private static void SaveAndFlush(TimeSpan wait)
        {
            try
            {
                ServiceProvider.GetRequiredService<CombatLogService>().EndSession();
                _ = ServiceProvider.GetRequiredService<DiceCombatsService>().SaveAllAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Saving on close failed: {ex}");
            }
            finally
            {
                // Whatever went wrong above, the saves already started are
                // waited for.
                if (wait > TimeSpan.Zero)
                {
                    try
                    {
                        var store = ServiceProvider.GetRequiredService<SafeFileStore>();
                        if (!Task.Run(() => store.FlushAsync()).Wait(wait))
                        {
                            Debug.WriteLine($"Saving on close did not finish within {wait.TotalSeconds} seconds.");
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Saving on close failed: {ex}");
                    }
                }
            }
        }
    }
}
