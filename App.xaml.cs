using System;
using System.IO;
using System.Windows;
using FocusFlow.Services;
using FocusFlow.Views;

namespace FocusFlow
{
    public partial class App : System.Windows.Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            AppDomain.CurrentDomain.UnhandledException += (s, ev) =>
            {
                string log = "UNHANDLED EXCEPTION: " + ev.ExceptionObject.ToString();
                Console.WriteLine(log);
                File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "crash.log"), log);
            };

            DispatcherUnhandledException += (s, ev) =>
            {
                string log = "DISPATCHER EXCEPTION: " + ev.Exception.ToString();
                Console.WriteLine(log);
                File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "crash.log"), log);
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

        protected override void OnExit(ExitEventArgs e)
        {
            ReminderService.Instance.Stop();
            NotificationService.Instance.Dispose();
            base.OnExit(e);
        }
    }
}
