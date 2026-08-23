using System;
using System.Windows.Threading;
using FocusFlow.Data;
using FocusFlow.Models;

namespace FocusFlow.Services
{
    public enum TimerState
    {
        Stopped,
        Running,
        Paused
    }

    public class PomodoroTimerService
    {
        private static PomodoroTimerService? _instance;
        public static PomodoroTimerService Instance => _instance ??= new PomodoroTimerService();

        private readonly DispatcherTimer _timer;
        private int _totalSeconds;
        private int _remainingSeconds;
        private TimerState _state = TimerState.Stopped;
        private SessionType _currentSessionType = SessionType.Focus;
        private TaskItem? _activeTask;
        private int _completedSessionCountInCycle = 0;
        private DateTime _sessionStartTime;

        public event Action? TimerTicked;
        public event Action? SessionCompleted;
        public event Action? StateChanged;

        public TimerState State => _state;
        public SessionType CurrentSessionType => _currentSessionType;
        public TaskItem? ActiveTask
        {
            get => _activeTask;
            set
            {
                _activeTask = value;
                StateChanged?.Invoke();
            }
        }

        public int RemainingSeconds => _remainingSeconds;
        public int TotalSeconds => _totalSeconds;
        public double ProgressFraction => _totalSeconds > 0 ? (double)(_totalSeconds - _remainingSeconds) / _totalSeconds : 0;
        public string TimeRemainingFormatted => $"{_remainingSeconds / 60:D2}:{_remainingSeconds % 60:D2}";

        public PomodoroTimerService()
        {
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _timer.Tick += Timer_Tick;

            ResetToMode(SessionType.Focus);
        }

        public void ResetToMode(SessionType type)
        {
            _timer.Stop();
            _state = TimerState.Stopped;
            _currentSessionType = type;

            var settings = DatabaseService.Instance.GetSettings();
            int minutes = type switch
            {
                SessionType.Focus => settings.FocusDurationMinutes,
                SessionType.ShortBreak => settings.ShortBreakMinutes,
                SessionType.LongBreak => settings.LongBreakMinutes,
                _ => settings.FocusDurationMinutes
            };

            _totalSeconds = minutes * 60;
            _remainingSeconds = _totalSeconds;

            StateChanged?.Invoke();
            TimerTicked?.Invoke();
        }

        public void StartTimer()
        {
            if (_state == TimerState.Running) return;

            if (_state == TimerState.Stopped)
            {
                _sessionStartTime = DateTime.Now;
            }

            _state = TimerState.Running;
            _timer.Start();
            StateChanged?.Invoke();
        }

        public void PauseTimer()
        {
            if (_state != TimerState.Running) return;

            _timer.Stop();
            _state = TimerState.Paused;
            StateChanged?.Invoke();
        }

        public void ResumeTimer()
        {
            StartTimer();
        }

        public void ResetTimer()
        {
            ResetToMode(_currentSessionType);
        }

        public void SkipSession()
        {
            _timer.Stop();
            SwitchToNextMode(autoStart: false);
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            if (_remainingSeconds > 0)
            {
                _remainingSeconds--;
                TimerTicked?.Invoke();
                NotificationService.Instance.UpdateTrayText($"FocusFlow: {TimeRemainingFormatted} ({_currentSessionType})");
            }
            else
            {
                OnSessionFinished();
            }
        }

        private void OnSessionFinished()
        {
            _timer.Stop();
            _state = TimerState.Stopped;

            var settings = DatabaseService.Instance.GetSettings();

            // Record completed session in database
            var session = new PomodoroSession
            {
                TaskId = _activeTask?.Id,
                Type = _currentSessionType,
                DurationMinutes = _totalSeconds / 60,
                StartedAt = _sessionStartTime,
                CompletedAt = DateTime.Now,
                IsSuccessful = true
            };
            DatabaseService.Instance.RecordPomodoroSession(session);

            if (_currentSessionType == SessionType.Focus)
            {
                _completedSessionCountInCycle++;
                if (_activeTask != null)
                {
                    _activeTask.CompletedPomodoros++;
                    DatabaseService.Instance.IncrementCompletedPomodoros(_activeTask.Id);
                }

                NotificationService.Instance.ShowNotification(
                    "Pomodoro Completed! 🎉",
                    _activeTask != null
                        ? $"Great job! Completed focus session for '{_activeTask.Title}'."
                        : "Focus session complete! Time to take a break."
                );
            }
            else
            {
                NotificationService.Instance.ShowNotification(
                    "Break Completed!",
                    "Ready to focus again? Select a task and hit start!"
                );
            }

            SessionCompleted?.Invoke();
            SwitchToNextMode(autoStart: _currentSessionType == SessionType.Focus ? settings.AutoStartBreaks : settings.AutoStartPomodoros);
        }

        private void SwitchToNextMode(bool autoStart)
        {
            var settings = DatabaseService.Instance.GetSettings();

            if (_currentSessionType == SessionType.Focus)
            {
                if (_completedSessionCountInCycle >= settings.LongBreakInterval)
                {
                    _completedSessionCountInCycle = 0;
                    ResetToMode(SessionType.LongBreak);
                }
                else
                {
                    ResetToMode(SessionType.ShortBreak);
                }
            }
            else
            {
                ResetToMode(SessionType.Focus);
            }

            if (autoStart)
            {
                StartTimer();
            }
        }
    }
}
