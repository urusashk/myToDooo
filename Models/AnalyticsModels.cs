using System;

namespace FocusFlow.Models
{
    public class ReportOverview
    {
        public int TotalFocusMinutes { get; set; }
        public int TodayFocusMinutes { get; set; }
        public int WeeklyFocusMinutes { get; set; }
        public int TotalPomodoros { get; set; }
        public int CompletedTasks { get; set; }
        public int PendingTasks { get; set; }
        public int OverdueTasks { get; set; }
        public int TotalTasks => CompletedTasks + PendingTasks;

        public int CompletionRatePercentage => TotalTasks > 0
            ? (int)Math.Round((double)CompletedTasks / TotalTasks * 100)
            : 0;
    }

    public class DailyTrendPoint
    {
        public DateTime Date { get; set; }
        public string DayLabel { get; set; } = string.Empty;
        public int FocusMinutes { get; set; }
        public int PomodorosCompleted { get; set; }
        public int TasksCompleted { get; set; }
        public double FocusHeightRatio { get; set; } = 0.0;
        public double PomodoroHeightRatio { get; set; } = 0.0;
        public double TasksHeightRatio { get; set; } = 0.0;
    }

    public class ProjectAnalytics
    {
        public int ProjectId { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public string ColorHex { get; set; } = "#3B82F6";
        public int TotalTasks { get; set; }
        public int CompletedTasks { get; set; }
        public int TotalFocusMinutes { get; set; }
        public int PomodorosCompleted { get; set; }

        public int CompletionPercentage => TotalTasks > 0
            ? (int)Math.Round((double)CompletedTasks / TotalTasks * 100)
            : 0;
    }
}
