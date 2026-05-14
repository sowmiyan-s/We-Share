using System;
using System.IO;
using Avalonia;

namespace WeShare.UI
{
    class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WeShare");
            try
            {
                if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
                File.AppendAllText(Path.Combine(folder, "startup.log"), $"[{DateTime.Now}] App Main entered.\n");

                App.PlatformService = new WeShare.Desktop.Services.WindowsPlatformService();
                BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
            }
            catch (Exception ex)
            {
                if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
                string logPath = Path.Combine(folder, "error.log");
                File.WriteAllText(logPath, $"[{DateTime.Now}] CRITICAL STARTUP ERROR:\n{ex.ToString()}");
                
                // Also try to show a message box if possible (Windows only)
                try {
                    System.Windows.Forms.MessageBox.Show($"We Share failed to start.\n\nError: {ex.Message}\n\nDetails logged to: {logPath}", "Startup Error", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
                } catch { }
            }
        }

        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .WithInterFont()
                .With(new Win32PlatformOptions
                {
                    // More compatible rendering on older machines
                    RenderingMode = new[] { Win32RenderingMode.Wgl, Win32RenderingMode.Software }
                })
                .LogToTrace();
    }
}
