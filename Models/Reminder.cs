using System;

namespace FocusFlow.Models
{
    public class Reminder
    {
        public int Id { get; set; }
        public int TaskId { get; set; }
        public string TaskTitle { get; set; } = string.Empty;
        public DateTime ReminderDateTime { get; set; }
        public string Message { get; set; } = string.Empty;
        public bool IsTriggered { get; set; }
    }
}
