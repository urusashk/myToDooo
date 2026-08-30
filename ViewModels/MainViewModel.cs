using System;
using System.Windows;
using System.Windows.Input;
using FocusFlow.Services;

namespace FocusFlow.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private static MainViewModel? _instance;
        public static MainViewModel Instance => _instance ??= new MainViewModel();

        private ViewModelBase _currentViewModel;
        private string _activeTabName = "Today Workspace";

        public DashboardViewModel DashboardViewModel { get; }
        public TasksViewModel TasksViewModel { get; }
        public PomodoroViewModel PomodoroViewModel { get; }
        public ProjectsViewModel ProjectsViewModel { get; }
        public CalendarViewModel CalendarViewModel { get; }
        public ReportsViewModel ReportsViewModel { get; }
        public SettingsViewModel SettingsViewModel { get; }

        public ViewModelBase CurrentViewModel
        {
            get => _currentViewModel;
            set
            {
                if (SetProperty(ref _currentViewModel, value))
                {
                    _currentViewModel.OnNavigatedTo();
                }
            }
        }

        public string ActiveTabName
        {
            get => _activeTabName;
            set => SetProperty(ref _activeTabName, value);
        }

        public ICommand NavigateDashboardCommand { get; }
        public ICommand NavigateTasksCommand { get; }
        public ICommand NavigatePomodoroCommand { get; }
        public ICommand NavigateProjectsCommand { get; }
        public ICommand NavigateCalendarCommand { get; }
        public ICommand NavigateReportsCommand { get; }
        public ICommand NavigateSettingsCommand { get; }

        public MainViewModel()
        {
            _instance = this;

            PomodoroViewModel = new PomodoroViewModel();
            DashboardViewModel = new DashboardViewModel();
            TasksViewModel = new TasksViewModel();
            ProjectsViewModel = new ProjectsViewModel();
            CalendarViewModel = new CalendarViewModel();
            ReportsViewModel = new ReportsViewModel();
            SettingsViewModel = new SettingsViewModel();

            _currentViewModel = DashboardViewModel;

            NavigationService.Instance.CurrentViewModelChanged += vm =>
            {
                CurrentViewModel = vm;
                UpdateActiveTabName(vm);
            };

            NavigateDashboardCommand = new RelayCommand(() => NavigationService.Instance.NavigateTo(DashboardViewModel));
            NavigateTasksCommand = new RelayCommand(() => NavigationService.Instance.NavigateTo(TasksViewModel));
            NavigatePomodoroCommand = new RelayCommand(() => NavigationService.Instance.NavigateTo(PomodoroViewModel));
            NavigateProjectsCommand = new RelayCommand(() => NavigationService.Instance.NavigateTo(ProjectsViewModel));
            NavigateCalendarCommand = new RelayCommand(() => NavigationService.Instance.NavigateTo(CalendarViewModel));
            NavigateReportsCommand = new RelayCommand(() => NavigationService.Instance.NavigateTo(ReportsViewModel));
            NavigateSettingsCommand = new RelayCommand(() => NavigationService.Instance.NavigateTo(SettingsViewModel));
        }

        private void UpdateActiveTabName(ViewModelBase vm)
        {
            if (vm is DashboardViewModel) ActiveTabName = "Today Workspace";
            else if (vm is TasksViewModel) ActiveTabName = "Tasks";
            else if (vm is PomodoroViewModel) ActiveTabName = "Focus Timer";
            else if (vm is ProjectsViewModel) ActiveTabName = "Projects";
            else if (vm is CalendarViewModel) ActiveTabName = "Calendar";
            else if (vm is ReportsViewModel) ActiveTabName = "Productivity Reports";
            else if (vm is SettingsViewModel) ActiveTabName = "Settings";
        }
    }
}
