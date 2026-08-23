using System;

namespace FocusFlow.Models
{
    public enum SessionType
    {
        Focus = 0,
        ShortBreak = 1,
        LongBreak = 2
    }

    public class PomodoroSession
    {
        public int Id { get; set; }
        public int? TaskId { get; set; }
        public string TaskTitle { get; set; } = string.Empty;
        public SessionType Type { get; set; } = SessionType.Focus;
        public int DurationMinutes { get; set; } = 25;
        public DateTime StartedAt { get; set; } = DateTime.Now;
        public DateTime CompletedAt { get; set; } = DateTime.Now;
        public bool IsSuccessful { get; set; } = true;

        public string TypeDisplay => Type switch
        {
            SessionType.Focus => "Focus Session",
            SessionType.ShortBreak => "Short Break",
            SessionType.LongBreak => "Long Break",
            _ => "Focus"
        };
    }
}
