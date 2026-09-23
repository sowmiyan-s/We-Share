using System;
using System.IO;
using Avalonia;

namespace WeShare.UI
{
    class Program
    {
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private const int SW_RESTORE = 9;

        [STAThread]
        public static void Main(string[] args)
        {
            using var mutex = new System.Threading.Mutex(true, "Local\\WeShare_SingleInstance_App_Mutex", out bool isNewInstance);
            if (!isNewInstance)
            {
                bool restoredWindow = false;
                try
                {
                    var current = System.Diagnostics.Process.GetCurrentProcess();
                    foreach (var p in System.Diagnostics.Process.GetProcessesByName(current.ProcessName))
                    {
                        if (p.Id != current.Id)
                        {
                            if (p.MainWindowHandle != IntPtr.Zero)
                            {
                                ShowWindow(p.MainWindowHandle, SW_RESTORE);
                                SetForegroundWindow(p.MainWindowHandle);
                                restoredWindow = true;
                                break;
                            }
                        }
                    }

                    // If an existing instance was detected but had NO visible window (headless/zombie),
                    // terminate the zombie process so this interactive launch can proceed
                    if (!restoredWindow)
                    {
                        foreach (var p in System.Diagnostics.Process.GetProcessesByName(current.ProcessName))
                        {
                            if (p.Id != current.Id)
                            {
                                try { p.Kill(); } catch { }
                            }
                        }
                    }
                }
                catch { }

                if (restoredWindow)
                    return;
            }

            string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WeShare");
            try
            {
                if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
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
                    RenderingMode = new[] { Win32RenderingMode.AngleEgl, Win32RenderingMode.Wgl, Win32RenderingMode.Software },
                    CompositionMode = new[] { Win32CompositionMode.WinUIComposition, Win32CompositionMode.LowLatencyDxgiSwapChain }
                })
                .LogToTrace();
    }
}
