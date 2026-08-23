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
    public class CalendarDay : ViewModelBase
    {
        public int DayNumber => Date.Day;
        public DateTime Date { get; set; }
        public bool IsToday => Date.Date == DateTime.Today;
        public bool IsCurrentMonth { get; set; }
        public ObservableCollection<TaskItem> Tasks { get; set; } = new();
    }

    public class CalendarViewModel : ViewModelBase
    {
        private DateTime _currentMonthYear = DateTime.Today;
        private ObservableCollection<CalendarDay> _days = new();
        private ObservableCollection<TaskItem> _overdueTasks = new();
        private ObservableCollection<TaskItem> _upcomingTasks = new();

        public string CurrentMonthYearHeader => _currentMonthYear.ToString("MMMM yyyy");

        public ObservableCollection<CalendarDay> Days
        {
            get => _days;
            set => SetProperty(ref _days, value);
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

        public ICommand PreviousMonthCommand { get; }
        public ICommand NextMonthCommand { get; }
        public ICommand TodayCommand { get; }
        public ICommand CreateTaskOnDateCommand { get; }
        public ICommand EditTaskCommand { get; }

        public CalendarViewModel()
        {
            PreviousMonthCommand = new RelayCommand(() => ChangeMonth(-1));
            NextMonthCommand = new RelayCommand(() => ChangeMonth(1));
            TodayCommand = new RelayCommand(() => { _currentMonthYear = DateTime.Today; BuildCalendar(); });
            CreateTaskOnDateCommand = new RelayCommand(CreateTaskOnDate);
            EditTaskCommand = new RelayCommand(EditTask);

            BuildCalendar();
        }

        public override void OnNavigatedTo()
        {
            BuildCalendar();
        }

        private void ChangeMonth(int months)
        {
            _currentMonthYear = _currentMonthYear.AddMonths(months);
            OnPropertyChanged(nameof(CurrentMonthYearHeader));
            BuildCalendar();
        }

        public void BuildCalendar()
        {
            var db = DatabaseService.Instance;
            var allTasks = db.GetTasks();
            var today = DateTime.Today;

            // Load Overdue Tasks
            var overdue = allTasks.Where(t => t.IsOverdue).OrderBy(t => t.DueDate).ToList();
            OverdueTasks.Clear();
            foreach (var t in overdue)
            {
                OverdueTasks.Add(t);
            }

            // Load Upcoming Tasks (next 7 days)
            var upcoming = allTasks.Where(t => !t.IsCompleted && t.DueDate.HasValue && t.DueDate.Value.Date >= today && t.DueDate.Value.Date <= today.AddDays(7)).OrderBy(t => t.DueDate).ToList();
            UpcomingTasks.Clear();
            foreach (var t in upcoming)
            {
                UpcomingTasks.Add(t);
            }

            // Build Month Grid
            Days.Clear();
            var firstDayOfMonth = new DateTime(_currentMonthYear.Year, _currentMonthYear.Month, 1);
            int dayOfWeek = (int)firstDayOfMonth.DayOfWeek; // 0 = Sunday

            var startDate = firstDayOfMonth.AddDays(-dayOfWeek);

            for (int i = 0; i < 42; i++) // 6 weeks * 7 days grid
            {
                var currentDate = startDate.AddDays(i);
                var dayTasks = allTasks.Where(t => t.DueDate.HasValue && t.DueDate.Value.Date == currentDate.Date).ToList();

                var dayObj = new CalendarDay
                {
                    Date = currentDate,
                    IsCurrentMonth = currentDate.Month == _currentMonthYear.Month
                };

                foreach (var t in dayTasks)
                {
                    dayObj.Tasks.Add(t);
                }

                Days.Add(dayObj);
            }
        }

        private void CreateTaskOnDate(object? parameter)
        {
            DateTime targetDate = DateTime.Today;
            if (parameter is CalendarDay day)
            {
                targetDate = day.Date;
            }
            else if (parameter is DateTime dt)
            {
                targetDate = dt;
            }

            var projects = DatabaseService.Instance.GetProjects();
            var newTask = new TaskItem
            {
                DueDate = targetDate,
                ProjectId = projects.FirstOrDefault()?.Id
            };

            var window = new TaskDialogWindow(newTask, projects)
            {
                Owner = System.Windows.Application.Current.MainWindow
            };

            if (window.ShowDialog() == true)
            {
                DatabaseService.Instance.SaveTask(window.TaskItem);
                BuildCalendar();
                MainViewModel.Instance.DashboardViewModel.LoadDashboardData();
                MainViewModel.Instance.TasksViewModel.LoadTasks();
            }
        }

        private void EditTask(object? parameter)
        {
            if (parameter is TaskItem task)
            {
                var projects = DatabaseService.Instance.GetProjects();
                var window = new TaskDialogWindow(task, projects)
                {
                    Owner = System.Windows.Application.Current.MainWindow
                };

                if (window.ShowDialog() == true)
                {
                    DatabaseService.Instance.SaveTask(window.TaskItem);
                    BuildCalendar();
                    MainViewModel.Instance.DashboardViewModel.LoadDashboardData();
                    MainViewModel.Instance.TasksViewModel.LoadTasks();
                }
            }
        }
    }
}
