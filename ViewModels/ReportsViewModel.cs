using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using FocusFlow.Data;
using FocusFlow.Models;

namespace FocusFlow.ViewModels
{
    public class DailyFocusStat
    {
        public string DayLabel { get; set; } = string.Empty;
        public int FocusMinutes { get; set; }
        public int PomodoroCount { get; set; }
        public double HeightRatio { get; set; }
    }

    public class ProjectFocusStat
    {
        public string ProjectName { get; set; } = string.Empty;
        public string ColorHex { get; set; } = "#3B82F6";
        public int TotalMinutes { get; set; }
        public int TaskCount { get; set; }
    }

    public class ReportsViewModel : ViewModelBase
    {
        private int _totalFocusMinutes;
        private int _totalPomodoros;
        private int _totalCompletedTasks;
        private ObservableCollection<DailyFocusStat> _weeklyStats = new();
        private ObservableCollection<ProjectFocusStat> _projectStats = new();

        public int TotalFocusMinutes
        {
            get => _totalFocusMinutes;
            set => SetProperty(ref _totalFocusMinutes, value);
        }

        public int TotalPomodoros
        {
            get => _totalPomodoros;
            set => SetProperty(ref _totalPomodoros, value);
        }

        public int TotalCompletedTasks
        {
            get => _totalCompletedTasks;
            set => SetProperty(ref _totalCompletedTasks, value);
        }

        public ObservableCollection<DailyFocusStat> WeeklyStats
        {
            get => _weeklyStats;
            set => SetProperty(ref _weeklyStats, value);
        }

        public ObservableCollection<ProjectFocusStat> ProjectStats
        {
            get => _projectStats;
            set => SetProperty(ref _projectStats, value);
        }

        public ReportsViewModel()
        {
            LoadReports();
        }

        public override void OnNavigatedTo()
        {
            LoadReports();
        }

        public void LoadReports()
        {
            var (_, _, totalCount, totalMinutes) = DatabaseService.Instance.GetFocusStats();
            TotalPomodoros = totalCount;
            TotalFocusMinutes = totalMinutes;

            var tasks = DatabaseService.Instance.GetTasks();
            TotalCompletedTasks = tasks.Count(t => t.IsCompleted);

            // Calculate past 7 days breakdown
            WeeklyStats.Clear();
            var sessions = DatabaseService.Instance.GetRecentSessions(200);

            int maxMinutes = 1;
            var dailyList = new List<DailyFocusStat>();

            for (int i = 6; i >= 0; i--)
            {
                DateTime day = DateTime.Today.AddDays(-i);
                int minutes = sessions.Where(s => s.StartedAt.Date == day.Date && s.Type == SessionType.Focus).Sum(s => s.DurationMinutes);
                int count = sessions.Count(s => s.StartedAt.Date == day.Date && s.Type == SessionType.Focus);
                
                if (minutes > maxMinutes) maxMinutes = minutes;

                dailyList.Add(new DailyFocusStat
                {
                    DayLabel = day.ToString("ddd"),
                    FocusMinutes = minutes,
                    PomodoroCount = count
                });
            }

            foreach (var item in dailyList)
            {
                item.HeightRatio = (double)item.FocusMinutes / maxMinutes;
                WeeklyStats.Add(item);
            }

            // Calculate Project stats
            ProjectStats.Clear();
            var projects = DatabaseService.Instance.GetProjects();
            foreach (var p in projects)
            {
                var pTasks = tasks.Where(t => t.ProjectId == p.Id).ToList();
                int pTaskIds = pTasks.Select(t => t.Id).Distinct().Count();
                int minutes = sessions.Where(s => s.TaskId.HasValue && pTasks.Any(t => t.Id == s.TaskId.Value)).Sum(s => s.DurationMinutes);

                ProjectStats.Add(new ProjectFocusStat
                {
                    ProjectName = p.Name,
                    ColorHex = p.ColorHex,
                    TotalMinutes = minutes,
                    TaskCount = pTaskIds
                });
            }
        }
    }
}
