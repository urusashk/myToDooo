using System;
using System.Media;
using System.Windows.Threading;
using FocusFlow.Data;
using FocusFlow.Models;

namespace FocusFlow.Services
{
    public class ReminderService
    {
        private static ReminderService? _instance;
        public static ReminderService Instance => _instance ??= new ReminderService();

        private readonly DispatcherTimer _timer;

        public ReminderService()
        {
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(5) // Checks pending reminders every 5 seconds
            };
            _timer.Tick += CheckPendingReminders;
        }

        public void Start()
        {
            if (!_timer.IsEnabled)
            {
                _timer.Start();
            }
        }

        public void Stop()
        {
            _timer.Stop();
        }

        private void CheckPendingReminders(object? sender, EventArgs e)
        {
            var db = DatabaseService.Instance;
            var settings = db.GetSettings();
            var pending = db.GetPendingReminders();

            foreach (var reminder in pending)
            {
                // 1. Show Windows Toast Notification
                if (settings.NotificationsEnabled)
                {
                    NotificationService.Instance.ShowNotification(
                        "📌 Task Reminder",
                        $"{reminder.TaskTitle}\n{reminder.Message}"
                    );
                }

                // 2. Play Audio Alert
                if (settings.SoundEnabled)
                {
                    SystemSounds.Exclamation.Play();
                }

                // 3. Handle Recurrence or One-Time De-duplication
                if (reminder.Recurrence == RecurringPattern.Daily)
                {
                    db.UpdateReminderDateTime(reminder.Id, reminder.ReminderDateTime.AddDays(1));
                }
                else if (reminder.Recurrence == RecurringPattern.Weekly)
                {
                    db.UpdateReminderDateTime(reminder.Id, reminder.ReminderDateTime.AddDays(7));
                }
                else if (reminder.Recurrence == RecurringPattern.Monthly)
                {
                    db.UpdateReminderDateTime(reminder.Id, reminder.ReminderDateTime.AddMonths(1));
                }
                else
                {
                    // One-Time Reminder: Mark as triggered in SQLite so it never re-fires on restart
                    db.MarkReminderTriggered(reminder.Id);
                }
            }
        }
    }
}
