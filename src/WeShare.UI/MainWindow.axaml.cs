using Avalonia.Controls;
using System;

namespace WeShare.UI
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            if (System.Linq.Enumerable.Contains(Environment.GetCommandLineArgs(), "--capture-screenshots", StringComparer.OrdinalIgnoreCase))
            {
                Opened += async (s, e) =>
                {
                    await System.Threading.Tasks.Task.Delay(1000);
                    await MainContent.CaptureScreenshotsForDocsAsync();
                    Environment.Exit(0);
                };
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            MainContent.Shutdown();
            base.OnClosed(e);
        }
    }
}
