using Avalonia.Controls;
using System;

namespace WeShare.UI
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        protected override void OnClosed(EventArgs e)
        {
            MainContent.Shutdown();
            base.OnClosed(e);
        }
    }
}
