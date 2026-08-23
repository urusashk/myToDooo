using System;
using System.Drawing;
using System.Windows.Forms;
using FocusFlow.Data;

namespace FocusFlow.Services
{
    public class NotificationService
    {
        private static NotificationService? _instance;
        public static NotificationService Instance => _instance ??= new NotificationService();

        private NotifyIcon? _notifyIcon;

        public void InitializeSystemTray(Action onOpenRequested, Action onExitRequested)
        {
            if (_notifyIcon != null) return;

            _notifyIcon = new NotifyIcon
            {
                Text = "FocusFlow - Pomodoro & Task Manager",
                Icon = SystemIcons.Application,
                Visible = true
            };

            var contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("Open FocusFlow", null, (s, e) => onOpenRequested?.Invoke());
            contextMenu.Items.Add("Start Focus Timer", null, (s, e) => {
                onOpenRequested?.Invoke();
                PomodoroTimerService.Instance.StartTimer();
            });
            contextMenu.Items.Add("-");
            contextMenu.Items.Add("Exit FocusFlow", null, (s, e) => onExitRequested?.Invoke());

            _notifyIcon.ContextMenuStrip = contextMenu;
            _notifyIcon.DoubleClick += (s, e) => onOpenRequested?.Invoke();
        }

        public void ShowNotification(string title, string message, ToolTipIcon icon = ToolTipIcon.Info)
        {
            var settings = DatabaseService.Instance.GetSettings();
            if (!settings.NotificationsEnabled) return;

            if (_notifyIcon != null)
            {
                _notifyIcon.ShowBalloonTip(3000, title, message, icon);
            }

            if (settings.SoundEnabled)
            {
                System.Media.SystemSounds.Beep.Play();
            }
        }

        public void UpdateTrayText(string text)
        {
            if (_notifyIcon != null)
            {
                // Truncate to maximum 63 characters allowed by NotifyIcon.Text
                _notifyIcon.Text = text.Length > 63 ? text.Substring(0, 60) + "..." : text;
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
