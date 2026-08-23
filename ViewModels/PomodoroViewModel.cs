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
        private ObservableCollection<TaskItem> _availableTasks = new();
        private TaskItem? _selectedTask;

        public PomodoroTimerService TimerService => PomodoroTimerService.Instance;

        public ObservableCollection<TaskItem> AvailableTasks
        {
            get => _availableTasks;
            set => SetProperty(ref _availableTasks, value);
        }

        public TaskItem? SelectedTask
        {
            get => _selectedTask;
            set
            {
                if (SetProperty(ref _selectedTask, value))
                {
                    TimerService.ActiveTask = value;
                }
            }
        }

        public ICommand StartCommand { get; }
        public ICommand PauseCommand { get; }
        public ICommand ResumeCommand { get; }
        public ICommand ResetCommand { get; }
        public ICommand SkipCommand { get; }
        public ICommand SetModeFocusCommand { get; }
        public ICommand SetModeShortBreakCommand { get; }
        public ICommand SetModeLongBreakCommand { get; }

        public PomodoroViewModel()
        {
            StartCommand = new RelayCommand(() => TimerService.StartTimer());
            PauseCommand = new RelayCommand(() => TimerService.PauseTimer());
            ResumeCommand = new RelayCommand(() => TimerService.ResumeTimer());
            ResetCommand = new RelayCommand(() => TimerService.ResetTimer());
            SkipCommand = new RelayCommand(() => TimerService.SkipSession());

            SetModeFocusCommand = new RelayCommand(() => TimerService.ResetToMode(SessionType.Focus));
            SetModeShortBreakCommand = new RelayCommand(() => TimerService.ResetToMode(SessionType.ShortBreak));
            SetModeLongBreakCommand = new RelayCommand(() => TimerService.ResetToMode(SessionType.LongBreak));

            TimerService.TimerTicked += () => OnPropertyChanged(nameof(TimerService));
            TimerService.StateChanged += () => OnPropertyChanged(nameof(TimerService));
            TimerService.SessionCompleted += () => OnPropertyChanged(nameof(TimerService));

            LoadTasks();
        }

        public override void OnNavigatedTo()
        {
            LoadTasks();
            if (TimerService.ActiveTask != null)
            {
                SelectedTask = AvailableTasks.FirstOrDefault(t => t.Id == TimerService.ActiveTask.Id);
            }
        }

        public void LoadTasks()
        {
            var tasks = DatabaseService.Instance.GetTasks(isCompleted: false);
            AvailableTasks.Clear();
            foreach (var t in tasks)
            {
                AvailableTasks.Add(t);
            }
        }
    }
}
