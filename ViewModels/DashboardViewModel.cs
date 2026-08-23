using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using FocusFlow.Data;
using FocusFlow.Models;
using FocusFlow.Services;

namespace FocusFlow.ViewModels
{
    public class DashboardViewModel : ViewModelBase
    {
        private int _completedTasksToday;
        private int _remainingTasksToday;
        private int _todayFocusMinutes;
        private int _todayPomodoros;

        // Quick Add Task properties
        private string _quickTaskTitle = string.Empty;
        private Project? _quickTaskProject;
        private TaskPriority _quickTaskPriority = TaskPriority.Medium;

        private ObservableCollection<TaskItem> _todayTasks = new();
        private ObservableCollection<TaskItem> _overdueTasks = new();
        private ObservableCollection<TaskItem> _upcomingTasks = new();
        private ObservableCollection<PomodoroSession> _recentSessions = new();
        private ObservableCollection<Project> _projects = new();

        public string TodayDateFormatted => DateTime.Today.ToString("dddd, MMMM dd, yyyy");

        public int CompletedTasksToday
        {
            get => _completedTasksToday;
            set => SetProperty(ref _completedTasksToday, value);
        }

        public int RemainingTasksToday
        {
            get => _remainingTasksToday;
            set => SetProperty(ref _remainingTasksToday, value);
        }

        public int TodayFocusMinutes
        {
            get => _todayFocusMinutes;
            set => SetProperty(ref _todayFocusMinutes, value);
        }

        public int TodayPomodoros
        {
            get => _todayPomodoros;
            set => SetProperty(ref _todayPomodoros, value);
        }

        public string QuickTaskTitle
        {
            get => _quickTaskTitle;
            set => SetProperty(ref _quickTaskTitle, value);
        }

        public Project? QuickTaskProject
        {
            get => _quickTaskProject;
            set => SetProperty(ref _quickTaskProject, value);
        }

        public TaskPriority QuickTaskPriority
        {
            get => _quickTaskPriority;
            set => SetProperty(ref _quickTaskPriority, value);
        }

        public ObservableCollection<TaskItem> TodayTasks
        {
            get => _todayTasks;
            set => SetProperty(ref _todayTasks, value);
        }

        public ObservableCollection<TaskItem> OverdueTasks
        {
            get => _overdueTasks;
            set => SetProperty(ref _overdueTasks, value);
        }

        public ObservableCollection<TaskItem> UpcomingTasks
        {
            get => _upcomingTasks;
            set => SetProperty(ref _upcomingTasks, value);
        }

        public ObservableCollection<PomodoroSession> RecentSessions
        {
            get => _recentSessions;
            set => SetProperty(ref _recentSessions, value);
        }

        public ObservableCollection<Project> Projects
        {
            get => _projects;
            set => SetProperty(ref _projects, value);
        }

        public Array Priorities => Enum.GetValues(typeof(TaskPriority));

        public ICommand QuickAddTaskCommand { get; }
        public ICommand StartFocusCommand { get; }
        public ICommand StartTaskFocusCommand { get; }
        public ICommand ToggleTaskCompletionCommand { get; }

        public DashboardViewModel()
        {
            QuickAddTaskCommand = new RelayCommand(QuickAddTask);
            StartFocusCommand = new RelayCommand(StartFocus);
            StartTaskFocusCommand = new RelayCommand(StartTaskFocus);
            ToggleTaskCompletionCommand = new RelayCommand(ToggleTaskCompletion);

            LoadDashboardData();
        }

        public override void OnNavigatedTo()
        {
            LoadDashboardData();
        }

        public void LoadDashboardData()
        {
            var db = DatabaseService.Instance;

            // Load Projects for Quick Add
            var dbProjects = db.GetProjects();
            Projects.Clear();
            foreach (var p in dbProjects)
            {
                Projects.Add(p);
            }
            if (QuickTaskProject == null && Projects.Count > 0)
            {
                QuickTaskProject = Projects.FirstOrDefault();
            }

            var allTasks = db.GetTasks();
            var today = DateTime.Today;

            // Load Overdue Tasks
            var overdue = allTasks.Where(t => t.IsOverdue).OrderBy(t => t.DueDate).ToList();
            OverdueTasks.Clear();
            foreach (var t in overdue)
            {
                OverdueTasks.Add(t);
            }

            // Load Today's Tasks
            var todayList = allTasks.Where(t => t.DueDate.HasValue && t.DueDate.Value.Date == today || t.CreatedAt.Date == today && !t.IsCompleted).ToList();
            TodayTasks.Clear();
            foreach (var t in todayList)
            {
                TodayTasks.Add(t);
            }

            // Load Upcoming Tasks
            var upcomingList = allTasks.Where(t => !t.IsCompleted && t.DueDate.HasValue && t.DueDate.Value.Date > today).OrderBy(t => t.DueDate).Take(5).ToList();
            UpcomingTasks.Clear();
            foreach (var t in upcomingList)
            {
                UpcomingTasks.Add(t);
            }

            // Metrics calculation
            CompletedTasksToday = allTasks.Count(t => t.IsCompleted && (t.CompletedAt.HasValue && t.CompletedAt.Value.Date == today || t.DueDate.HasValue && t.DueDate.Value.Date == today));
            RemainingTasksToday = allTasks.Count(t => !t.IsCompleted && (t.DueDate.HasValue && t.DueDate.Value.Date <= today));

            var (todayCount, todayMins, _, _) = db.GetFocusStats();
            TodayPomodoros = todayCount;
            TodayFocusMinutes = todayMins;

            // Load Recent Sessions
            var sessions = db.GetRecentSessions(5);
            RecentSessions.Clear();
            foreach (var s in sessions)
            {
                RecentSessions.Add(s);
            }
        }

        private void QuickAddTask()
        {
            if (string.IsNullOrWhiteSpace(QuickTaskTitle)) return;

            var newTask = new TaskItem
            {
                Title = QuickTaskTitle.Trim(),
                Priority = QuickTaskPriority,
                DueDate = DateTime.Today,
                ProjectId = QuickTaskProject?.Id,
                ProjectName = QuickTaskProject?.Name ?? "Inbox",
                ProjectColor = QuickTaskProject?.ColorHex ?? "#64748B",
                EstimatedPomodoros = 1,
                CreatedAt = DateTime.Now
            };

            DatabaseService.Instance.SaveTask(newTask);

            QuickTaskTitle = string.Empty;
            LoadDashboardData();

            // Also refresh Tasks list and Calendar
            MainViewModel.Instance.TasksViewModel.LoadTasks();
            MainViewModel.Instance.CalendarViewModel.BuildCalendar();
        }

        private void StartFocus()
        {
            var firstTask = TodayTasks.FirstOrDefault(t => !t.IsCompleted);
            if (firstTask != null)
            {
                MainViewModel.Instance.PomodoroViewModel.SelectedTask = firstTask;
            }
            NavigationService.Instance.NavigateTo(MainViewModel.Instance.PomodoroViewModel);
        }

        private void StartTaskFocus(object? parameter)
        {
            if (parameter is TaskItem task)
            {
                MainViewModel.Instance.PomodoroViewModel.SelectedTask = task;
                NavigationService.Instance.NavigateTo(MainViewModel.Instance.PomodoroViewModel);
            }
        }

        private void ToggleTaskCompletion(object? parameter)
        {
            if (parameter is TaskItem task)
            {
                task.IsCompleted = !task.IsCompleted;
                DatabaseService.Instance.ToggleTaskCompletion(task.Id, task.IsCompleted);
                LoadDashboardData();
                MainViewModel.Instance.CalendarViewModel.BuildCalendar();
            }
        }
    }
}
