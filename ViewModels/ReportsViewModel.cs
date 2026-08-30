using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using FocusFlow.Data;
using FocusFlow.Models;

namespace FocusFlow.ViewModels
{
    public class ReportsViewModel : ViewModelBase
    {
        private string _selectedFilterMode = "This Week"; // Today, This Week, This Month, Custom
        private DateTime _customStartDate = DateTime.Today.AddDays(-7);
        private DateTime _customEndDate = DateTime.Today;

        private ReportOverview _overview = new();
        private ObservableCollection<DailyTrendPoint> _dailyTrends = new();
        private ObservableCollection<ProjectAnalytics> _projectAnalyticsList = new();

        private bool _hasFocusData;
        private bool _hasProjectData;

        public string SelectedFilterMode
        {
            get => _selectedFilterMode;
            set
            {
                if (SetProperty(ref _selectedFilterMode, value))
                {
                    LoadReports();
                }
            }
        }

        public DateTime CustomStartDate
        {
            get => _customStartDate;
            set
            {
                if (SetProperty(ref _customStartDate, value))
                {
                    if (SelectedFilterMode == "Custom") LoadReports();
                }
            }
        }

        public DateTime CustomEndDate
        {
            get => _customEndDate;
            set
            {
                if (SetProperty(ref _customEndDate, value))
                {
                    if (SelectedFilterMode == "Custom") LoadReports();
                }
            }
        }

        public ReportOverview Overview
        {
            get => _overview;
            set => SetProperty(ref _overview, value);
        }

        public ObservableCollection<DailyTrendPoint> DailyTrends
        {
            get => _dailyTrends;
            set => SetProperty(ref _dailyTrends, value);
        }

        public ObservableCollection<ProjectAnalytics> ProjectAnalyticsList
        {
            get => _projectAnalyticsList;
            set => SetProperty(ref _projectAnalyticsList, value);
        }

        public bool HasFocusData
        {
            get => _hasFocusData;
            set => SetProperty(ref _hasFocusData, value);
        }

        public bool HasProjectData
        {
            get => _hasProjectData;
            set => SetProperty(ref _hasProjectData, value);
        }

        public ICommand SelectFilterModeCommand { get; }
        public ICommand RefreshReportCommand { get; }

        public ReportsViewModel()
        {
            SelectFilterModeCommand = new RelayCommand(SelectFilterMode);
            RefreshReportCommand = new RelayCommand(LoadReports);

            LoadReports();
        }

        public override void OnNavigatedTo()
        {
            LoadReports();
        }

        private void SelectFilterMode(object? parameter)
        {
            if (parameter is string mode)
            {
                SelectedFilterMode = mode;
            }
        }

        public void LoadReports()
        {
            var db = DatabaseService.Instance;
            DateTime startDate;
            DateTime endDate;

            var today = DateTime.Today;

            switch (SelectedFilterMode)
            {
                case "Today":
                    startDate = today;
                    endDate = today;
                    break;
                case "This Month":
                    startDate = new DateTime(today.Year, today.Month, 1);
                    endDate = today;
                    break;
                case "Custom":
                    startDate = CustomStartDate;
                    endDate = CustomEndDate;
                    break;
                case "This Week":
                default:
                    int dayOfWeek = (int)today.DayOfWeek;
                    startDate = today.AddDays(-dayOfWeek); // Start of week (Sunday)
                    endDate = today;
                    break;
            }

            // 1. Fetch Overview Stats
            Overview = db.GetReportOverview(startDate, endDate);

            // 2. Fetch Daily Trends for Bar Charts
            var trends = db.GetDailyTrends(startDate, endDate);
            DailyTrends.Clear();
            foreach (var point in trends)
            {
                DailyTrends.Add(point);
            }

            // 3. Fetch Project Analytics
            var projList = db.GetProjectAnalytics(startDate, endDate);
            ProjectAnalyticsList.Clear();
            foreach (var proj in projList)
            {
                ProjectAnalyticsList.Add(proj);
            }

            // 4. Update Empty State Flags
            HasFocusData = Overview.TotalFocusMinutes > 0 || DailyTrends.Any(t => t.FocusMinutes > 0);
            HasProjectData = ProjectAnalyticsList.Any(p => p.TotalTasks > 0 || p.TotalFocusMinutes > 0);
        }
    }
}
