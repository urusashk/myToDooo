using System;
using System.ComponentModel;
using System.Windows;
using FocusFlow.Services;

namespace FocusFlow.Views
{
    public partial class MainWindow : System.Windows.Window
    {
        public bool IsExplicitExit { get; set; } = false;

        public MainWindow()
        {
            InitializeComponent();
        }

        protected override void OnStateChanged(EventArgs e)
        {
            base.OnStateChanged(e);
            if (WindowState == WindowState.Minimized)
            {
                // Hide main window to keep system tray clean and background timer active
                Hide();
                NotificationService.Instance.ShowNotification(
                    "FocusFlow Running in Tray",
                    "Your Pomodoro timer is continuing to count accurately in the background."
                );
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (!IsExplicitExit)
            {
                // Minimize to tray instead of quitting
                e.Cancel = true;
                Hide();
                NotificationService.Instance.ShowNotification(
                    "FocusFlow Running in Background",
                    "Double click tray icon to reopen."
                );
            }
            else
            {
                base.OnClosing(e);
            }
        }
    }
}
