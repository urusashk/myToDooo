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
        private static readonly string CrashLogPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FocusFlow",
            "crash.log"
        );

        protected override void OnStartup(StartupEventArgs e)
        {
            // CRITICAL FIX: Prevent WPF from shutting down when MainWindow is hidden or minimized
            ShutdownMode = ShutdownMode.OnExplicitShutdown;

            Directory.CreateDirectory(Path.GetDirectoryName(CrashLogPath)!);

            AppDomain.CurrentDomain.UnhandledException += (s, ev) =>
            {
                LogException("AppDomain.UnhandledException", ev.ExceptionObject as Exception);
            };

            DispatcherUnhandledException += (s, ev) =>
            {
                LogException("DispatcherUnhandledException", ev.Exception);
            };

            TaskScheduler.UnobservedTaskException += (s, ev) =>
            {
                LogException("TaskScheduler.UnobservedTaskException", ev.Exception);
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
                        if (Current.MainWindow is MainWindow mw)
                        {
                            mw.IsExplicitExit = true;
                        }
                        NotificationService.Instance.Dispose();
                        Current.Shutdown();
                    });
                }
            );
        }

        private static void LogException(string source, Exception? ex)
        {
            if (ex == null) return;
            string content = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{source}]\n{ex}\n\n";
            File.AppendAllText(CrashLogPath, content);
            Console.WriteLine(content);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            ReminderService.Instance.Stop();
            NotificationService.Instance.Dispose();
            base.OnExit(e);
        }
    }
}
