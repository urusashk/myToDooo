using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using FocusFlow.Services;
using FocusFlow.Views;

namespace FocusFlow
{
    public partial class App : System.Windows.Application
    {
        private static readonly string LifecycleLogPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FocusFlow",
            "lifecycle.log"
        );

        protected override void OnStartup(StartupEventArgs e)
        {
            LogLifecycle("App.OnStartup BEGIN");
            ShutdownMode = ShutdownMode.OnExplicitShutdown;

            AppDomain.CurrentDomain.UnhandledException += (s, ev) =>
            {
                LogLifecycle("AppDomain.UnhandledException: " + ev.ExceptionObject);
            };

            DispatcherUnhandledException += (s, ev) =>
            {
                LogLifecycle("DispatcherUnhandledException: " + ev.Exception);
            };

            TaskScheduler.UnobservedTaskException += (s, ev) =>
            {
                LogLifecycle("TaskScheduler.UnobservedTaskException: " + ev.Exception);
            };

            base.OnStartup(e);

            // Start Background Reminder Engine
            ReminderService.Instance.Start();

            NotificationService.Instance.InitializeSystemTray(
                onOpenRequested: () =>
                {
                    Current.Dispatcher.Invoke(() =>
                    {
                        if (Current.MainWindow != null)
                        {
                            Current.MainWindow.Show();
                            Current.MainWindow.WindowState = WindowState.Normal;
                            Current.MainWindow.Activate();
                        }
                    });
                },
                onExitRequested: () =>
                {
                    Current.Dispatcher.Invoke(() =>
                    {
                        LogLifecycle("Tray Exit Requested");
                        if (Current.MainWindow is MainWindow mw)
                        {
                            mw.IsExplicitExit = true;
                        }
                        NotificationService.Instance.Dispose();
                        Current.Shutdown();
                    });
                }
            );

            LogLifecycle("App.OnStartup END");
        }

        public static void LogLifecycle(string msg)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(LifecycleLogPath)!);
                string line = $"[{DateTime.Now:HH:mm:ss.fff}] [App] {msg}\n";
                File.AppendAllText(LifecycleLogPath, line);
            }
            catch { }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            LogLifecycle($"App.OnExit: Application terminating with exit code {e.ApplicationExitCode}");
            ReminderService.Instance.Stop();
            NotificationService.Instance.Dispose();
            base.OnExit(e);
        }
    }
}
