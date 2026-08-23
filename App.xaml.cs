using System;
using System.Windows;
using FocusFlow.Services;

namespace FocusFlow
{
    public partial class App : System.Windows.Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

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
                        NotificationService.Instance.Dispose();
                        Current.Shutdown();
                    });
                }
            );
        }

        protected override void OnExit(ExitEventArgs e)
        {
            NotificationService.Instance.Dispose();
            base.OnExit(e);
        }
    }
}
