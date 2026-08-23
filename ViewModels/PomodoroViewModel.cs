using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using FocusFlow.Data;
using FocusFlow.Models;
using FocusFlow.Services;

namespace FocusFlow.ViewModels
{
    public class PomodoroViewModel : ViewModelBase
    {
        private TaskItem? _selectedTask;
        private ObservableCollection<TaskItem> _availableTasks = new();
        private bool _isTestMode;

        public PomodoroTimerService TimerService => PomodoroTimerService.Instance;

        public TaskItem? SelectedTask
        {
            get => _selectedTask;
            set
            {
                if (SetProperty(ref _selectedTask, value))
                {
                    TimerService.CurrentTask = value;
                }
            }
        }

        public ObservableCollection<TaskItem> AvailableTasks
        {
            get => _availableTasks;
            set => SetProperty(ref _availableTasks, value);
        }

        public bool IsTestMode
        {
            get => _isTestMode;
            set
            {
                if (SetProperty(ref _isTestMode, value))
                {
                    TimerService.SetTestMode(value);
                }
            }
        }

        public ICommand StartCommand { get; }
        public ICommand PauseCommand { get; }
        public ICommand ResetCommand { get; }
        public ICommand SkipCommand { get; }
        public ICommand SetModeFocusCommand { get; }
        public ICommand SetModeShortBreakCommand { get; }
        public ICommand SetModeLongBreakCommand { get; }
        public ICommand ToggleTestModeCommand { get; }

        public PomodoroViewModel()
        {
            StartCommand = new RelayCommand(() => TimerService.Start());
            PauseCommand = new RelayCommand(() => TimerService.Pause());
            ResetCommand = new RelayCommand(() => TimerService.Reset());
            SkipCommand = new RelayCommand(() => TimerService.Skip());

            SetModeFocusCommand = new RelayCommand(() => TimerService.ResetToMode(SessionType.Focus));
            SetModeShortBreakCommand = new RelayCommand(() => TimerService.ResetToMode(SessionType.ShortBreak));
            SetModeLongBreakCommand = new RelayCommand(() => TimerService.ResetToMode(SessionType.LongBreak));
            ToggleTestModeCommand = new RelayCommand(() => IsTestMode = !IsTestMode);

            TimerService.Tick += OnTimerTick;
            TimerService.StateChanged += OnTimerStateChanged;

            LoadAvailableTasks();
        }

        public override void OnNavigatedTo()
        {
            LoadAvailableTasks();
        }

        public void LoadAvailableTasks()
        {
            var tasks = DatabaseService.Instance.GetTasks(isCompleted: false);
            AvailableTasks.Clear();
            foreach (var t in tasks)
            {
                AvailableTasks.Add(t);
            }

            if (SelectedTask == null && AvailableTasks.Count > 0)
            {
                SelectedTask = AvailableTasks.First();
            }
        }

        private void OnTimerTick()
        {
            OnPropertyChanged(nameof(TimerService));
        }

        private void OnTimerStateChanged()
        {
            OnPropertyChanged(nameof(TimerService));
        }
    }
}
