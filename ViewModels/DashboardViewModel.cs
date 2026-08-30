using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using FocusFlow.Data;
using FocusFlow.Models;
using FocusFlow.Services;
using FocusFlow.Views;

namespace FocusFlow.ViewModels
{
    public class DashboardViewModel : ViewModelBase
    {
        private ObservableCollection<TaskItem> _todayTasks = new();
        private ObservableCollection<Project> _projects = new();

        public string TodayDateFormatted => DateTime.Today.ToString("dddd, MMMM dd, yyyy");

        public ObservableCollection<TaskItem> TodayTasks
        {
            get => _todayTasks;
            set => SetProperty(ref _todayTasks, value);
        }

        public ObservableCollection<Project> Projects
        {
            get => _projects;
            set => SetProperty(ref _projects, value);
        }

        public PomodoroViewModel? PomodoroViewModel => MainViewModel.Instance?.PomodoroViewModel;

        public ICommand CreateTaskCommand { get; }
        public ICommand StartTaskFocusCommand { get; }
        public ICommand ToggleTaskCompletionCommand { get; }

        public DashboardViewModel()
        {
            CreateTaskCommand = new RelayCommand(CreateTask);
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

            // Load Projects
            var dbProjects = db.GetProjects();
            Projects.Clear();
            foreach (var p in dbProjects)
            {
                Projects.Add(p);
            }

            var allTasks = db.GetTasks();
            var today = DateTime.Today;

            // Load Today's Tasks: ACTIVE FIRST (IsCompleted = false), COMPLETED AT BOTTOM (IsCompleted = true)
            var todayList = allTasks.Where(t => t.DueDate.HasValue && t.DueDate.Value.Date == today || t.CreatedAt.Date == today && !t.IsCompleted)
                                     .OrderBy(t => t.IsCompleted)
                                     .ThenByDescending(t => t.Priority)
                                     .ThenBy(t => t.DueDate)
                                     .ToList();

            TodayTasks.Clear();
            foreach (var t in todayList)
            {
                TodayTasks.Add(t);
            }

            // Set default selected task on PomodoroViewModel if available and none selected
            var pomodoroVm = MainViewModel.Instance?.PomodoroViewModel;
            if (pomodoroVm != null && pomodoroVm.SelectedTask == null)
            {
                var activeTodayTask = TodayTasks.FirstOrDefault(t => !t.IsCompleted);
                if (activeTodayTask != null)
                {
                    pomodoroVm.SelectedTask = activeTodayTask;
                }
            }
        }

        private void CreateTask()
        {
            var newTask = new TaskItem
            {
                DueDate = DateTime.Today,
                ProjectId = Projects.FirstOrDefault(p => p.Id > 0)?.Id
            };

            var window = new TaskDialogWindow(newTask, Projects.Where(p => p.Id > 0))
            {
                Owner = System.Windows.Application.Current.MainWindow
            };

            if (window.ShowDialog() == true)
            {
                DatabaseService.Instance.SaveTask(window.TaskItem);
                LoadDashboardData();
                MainViewModel.Instance.TasksViewModel.LoadTasks();
                MainViewModel.Instance.CalendarViewModel.BuildCalendar();
            }
        }

        private void StartTaskFocus(object? parameter)
        {
            if (parameter is TaskItem task)
            {
                var pomodoroVm = MainViewModel.Instance?.PomodoroViewModel;
                if (pomodoroVm != null)
                {
                    pomodoroVm.SelectedTask = task;
                }
            }
        }

        private void ToggleTaskCompletion(object? parameter)
        {
            if (parameter is TaskItem task)
            {
                task.IsCompleted = !task.IsCompleted;
                DatabaseService.Instance.ToggleTaskCompletion(task.Id, task.IsCompleted);
                LoadDashboardData();
                MainViewModel.Instance.TasksViewModel.LoadTasks();
                MainViewModel.Instance.CalendarViewModel.BuildCalendar();
            }
        }
    }
}
