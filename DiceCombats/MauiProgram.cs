﻿using Microsoft.Extensions.Logging;
using MudBlazor.Services;

namespace DiceCombats
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                });

            builder.Services.AddMauiBlazorWebView();

#if DEBUG
    		builder.Services.AddBlazorWebViewDeveloperTools();
    		builder.Logging.AddDebug();
#endif

            // MudBlazor
            builder.Services.AddMudServices();

            builder.Services.AddSingleton<DiceCombatsService>();

            return builder.Build();
        }
    }
}
