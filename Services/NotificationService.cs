using System;
using System.Drawing;
using System.Windows.Forms;

namespace FocusFlow.Services
{
    public class NotificationService : IDisposable
    {
        private static NotificationService? _instance;
        public static NotificationService Instance => _instance ??= new NotificationService();

        private NotifyIcon? _notifyIcon;
        private Action? _onOpenRequested;
        private Action? _onExitRequested;

        public void InitializeSystemTray(Action onOpenRequested, Action onExitRequested)
        {
            _onOpenRequested = onOpenRequested;
            _onExitRequested = onExitRequested;

            if (_notifyIcon == null)
            {
                _notifyIcon = new NotifyIcon
                {
                    Icon = SystemIcons.Application,
                    Text = "My Tasks - Pomodoro & Tasks",
                    Visible = true
                };

                var contextMenu = new ContextMenuStrip();
                
                var openItem = new ToolStripMenuItem("Open My Tasks", null, (s, e) => _onOpenRequested?.Invoke());
                openItem.Font = new Font(openItem.Font, FontStyle.Bold);
                contextMenu.Items.Add(openItem);

                contextMenu.Items.Add(new ToolStripSeparator());

                contextMenu.Items.Add("Pause / Resume Timer", null, (s, e) =>
                {
                    var timer = PomodoroTimerService.Instance;
                    if (timer.State == TimerState.Running)
                        timer.Pause();
                    else
                        timer.Start();
                });

                contextMenu.Items.Add("Reset Timer", null, (s, e) =>
                {
                    PomodoroTimerService.Instance.Reset();
                });

                contextMenu.Items.Add(new ToolStripSeparator());

                contextMenu.Items.Add("Exit My Tasks", null, (s, e) => _onExitRequested?.Invoke());

                _notifyIcon.ContextMenuStrip = contextMenu;
                _notifyIcon.DoubleClick += (s, e) => _onOpenRequested?.Invoke();
            }
        }

        public void ShowNotification(string title, string message)
        {
            if (_notifyIcon != null)
            {
                _notifyIcon.ShowBalloonTip(5000, title, message, ToolTipIcon.Info);
            }
        }

        public void Dispose()
        {
            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = false;
                _notifyIcon.Dispose();
                _notifyIcon = null;
            }
        }
    }
}
