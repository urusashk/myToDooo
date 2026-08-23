namespace FocusFlow.Models
{
    public class UserSettings
    {
        public int Id { get; set; } = 1;
        public int FocusDurationMinutes { get; set; } = 25;
        public int ShortBreakMinutes { get; set; } = 5;
        public int LongBreakMinutes { get; set; } = 15;
        public int LongBreakInterval { get; set; } = 4;
        public bool SoundEnabled { get; set; } = true;
        public bool NotificationsEnabled { get; set; } = true;
        public bool LaunchAtStartup { get; set; } = false;
        public bool AutoStartBreaks { get; set; } = false;
        public bool AutoStartPomodoros { get; set; } = false;
        public string Theme { get; set; } = "Dark";
    }
}
