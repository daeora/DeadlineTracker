using Microsoft.Extensions.Logging;
using Microsoft.Maui;
using Microsoft.Maui.Controls.Hosting;
using Microsoft.Maui.Hosting;

#if WINDOWS
using Microsoft.Maui.LifecycleEvents;
using Microsoft.UI.Windowing;
#endif

namespace DeadlineTracker
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
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                    fonts.AddFont("MaterialIcons-Regular.ttf", "MaterialIcons");
                });

#if WINDOWS
            // 1) Poistetaan Entryjen sininen alaviiva / border
            Microsoft.Maui.Handlers.EntryHandler.Mapper.AppendToMapping(
                "NoUnderline", (handler, view) =>
                {
                    var nativeEntry = handler.PlatformView;

                    nativeEntry.BorderThickness = new Microsoft.UI.Xaml.Thickness(0);
                    nativeEntry.Style = null;
                });

            // 2) Muokataan Windows-ikkunan otsikkopalkkia
            builder.ConfigureLifecycleEvents(events =>
            {
                events.AddWindows(windows =>
                {
                    windows.OnWindowCreated(window =>
                    {
                        var appWindow = window.AppWindow;
                        appWindow.Title = "Deadline Tracker";

                        var titleBar = appWindow.TitleBar;

                        var bg = Microsoft.UI.Colors.White;       // aina vaalea tausta
                        var fg = Microsoft.UI.Colors.DarkBlue;    // tumma teksti
                        var darkBg = Microsoft.UI.Colors.Black;  // musta exitille

                        // Aktiivinen ikkuna
                        titleBar.BackgroundColor       = bg;
                        titleBar.ForegroundColor       = fg;
                        titleBar.ButtonBackgroundColor = darkBg;
                        titleBar.ButtonForegroundColor = bg;

                        // Ei-aktiivinen ikkuna
                        titleBar.InactiveBackgroundColor       = bg;
                        titleBar.InactiveForegroundColor       = fg;
                        titleBar.ButtonInactiveBackgroundColor = darkBg;
                        titleBar.ButtonInactiveForegroundColor = bg;

                        // (Halutessa myös hover/pressed samaksi)
                        titleBar.ButtonHoverBackgroundColor   = bg;
                        titleBar.ButtonPressedBackgroundColor = bg;
                    });
                });
            });
#endif

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}