using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using FocusFlow.Data;
using FocusFlow.Models;

namespace FocusFlow.ViewModels
{
    public class CalendarDayItem
    {
        public DateTime Date { get; set; }
        public int DayNumber => Date.Day;
        public bool IsCurrentMonth { get; set; }
        public bool IsToday => Date.Date == DateTime.Today;
        public List<TaskItem> Tasks { get; set; } = new();
        public int CompletedTaskCount => Tasks.Count(t => t.IsCompleted);
    }

    public class CalendarViewModel : ViewModelBase
    {
        private DateTime _currentMonth = DateTime.Today;
        private ObservableCollection<CalendarDayItem> _days = new();

        public string CurrentMonthYearHeader => _currentMonth.ToString("MMMM yyyy");

        public ObservableCollection<CalendarDayItem> Days
        {
            get => _days;
            set => SetProperty(ref _days, value);
        }

        public ICommand PreviousMonthCommand { get; }
        public ICommand NextMonthCommand { get; }
        public ICommand TodayCommand { get; }

        public CalendarViewModel()
        {
            PreviousMonthCommand = new RelayCommand(() => ChangeMonth(-1));
            NextMonthCommand = new RelayCommand(() => ChangeMonth(1));
            TodayCommand = new RelayCommand(GoToToday);

            BuildCalendar();
        }

        public override void OnNavigatedTo()
        {
            BuildCalendar();
        }

        private void ChangeMonth(int offset)
        {
            _currentMonth = _currentMonth.AddMonths(offset);
            OnPropertyChanged(nameof(CurrentMonthYearHeader));
            BuildCalendar();
        }

        private void GoToToday()
        {
            _currentMonth = DateTime.Today;
            OnPropertyChanged(nameof(CurrentMonthYearHeader));
            BuildCalendar();
        }

        public void BuildCalendar()
        {
            var allTasks = DatabaseService.Instance.GetTasks();

            Days.Clear();

            DateTime firstOfMonth = new DateTime(_currentMonth.Year, _currentMonth.Month, 1);
            int daysInMonth = DateTime.DaysInMonth(_currentMonth.Year, _currentMonth.Month);
            int startOffset = (int)firstOfMonth.DayOfWeek; // 0 = Sunday

            DateTime calendarStartDate = firstOfMonth.AddDays(-startOffset);

            for (int i = 0; i < 35; i++)
            {
                DateTime dayDate = calendarStartDate.AddDays(i);
                bool isCurrentMonth = dayDate.Month == _currentMonth.Month;

                var dayTasks = allTasks.Where(t => t.DueDate.HasValue && t.DueDate.Value.Date == dayDate.Date).ToList();

                Days.Add(new CalendarDayItem
                {
                    Date = dayDate,
                    IsCurrentMonth = isCurrentMonth,
                    Tasks = dayTasks
                });
            }
        }
    }
}
