using System;
using Avalonia;

namespace WeShare.UI
{
    class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            App.PlatformService = new WeShare.Desktop.Services.WindowsPlatformService();
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }

        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .WithInterFont()
                .LogToTrace();
    }
}
