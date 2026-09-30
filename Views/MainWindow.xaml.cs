using System;
using System.ComponentModel;
using System.IO;
using System.Windows;
using FocusFlow.Services;

namespace FocusFlow.Views
{
    public partial class MainWindow : System.Windows.Window
    {
        public bool IsExplicitExit { get; set; } = false;

        private static readonly string LifecycleLogPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FocusFlow",
            "lifecycle.log"
        );

        public MainWindow()
        {
            InitializeComponent();

            IsVisibleChanged += (s, e) =>
            {
                LogLifecycle($"IsVisibleChanged: NewValue={e.NewValue}, WindowState={WindowState}");
            };

            Closed += (s, e) =>
            {
                LogLifecycle($"Closed event fired. IsExplicitExit={IsExplicitExit}");
            };
        }

        protected override void OnStateChanged(EventArgs e)
        {
            base.OnStateChanged(e);
            LogLifecycle($"OnStateChanged: WindowState={WindowState}");

            // Standard Windows minimize behavior:
            // Do NOT call Hide() on minimize so window minimizes to taskbar normally,
            // process stays alive in Task Manager, and timers continue running.
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            LogLifecycle($"OnClosing: IsExplicitExit={IsExplicitExit}");
            if (!IsExplicitExit)
            {
                // Close button (X) hides window to System Tray instead of quitting process
                e.Cancel = true;
                Hide();
                LogLifecycle("OnClosing: Window hidden to System Tray. Process remaining active.");
                NotificationService.Instance.ShowNotification(
                    "FocusFlow Running in Tray",
                    "Your Pomodoro timer and reminders are continuing in the background."
                );
            }
            else
            {
                base.OnClosing(e);
                LogLifecycle("OnClosing: Explicit exit allowed.");
            }
        }

        public static void LogLifecycle(string msg)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(LifecycleLogPath)!);
                string line = $"[{DateTime.Now:HH:mm:ss.fff}] {msg}\n";
                File.AppendAllText(LifecycleLogPath, line);
            }
            catch { }
        }
    }
}
