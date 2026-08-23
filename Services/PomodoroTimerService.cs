using System;
using System.Media;
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
        private DateTime _targetEndTimeUtc;
        private TimeSpan _remainingTimeSpan;
        private TimeSpan _totalDurationTimeSpan;
        private DateTime _sessionStartTimeUtc;

        public SessionType CurrentSessionType { get; private set; } = SessionType.Focus;
        public TimerState State { get; private set; } = TimerState.Stopped;

        public int CompletedFocusSessionsCount { get; private set; } = 0;
        public TaskItem? CurrentTask { get; set; }

        public bool IsTestMode { get; set; } = false;

        public string TimeRemainingFormatted
        {
            get
            {
                var ts = GetRemainingTimeSpan();
                return $"{Math.Max(0, (int)ts.TotalMinutes):D2}:{Math.Max(0, ts.Seconds):D2}";
            }
        }

        public double ProgressRatio
        {
            get
            {
                if (_totalDurationTimeSpan.TotalSeconds <= 0) return 0;
                var remaining = GetRemainingTimeSpan().TotalSeconds;
                var ratio = 1.0 - (remaining / _totalDurationTimeSpan.TotalSeconds);
                return Math.Clamp(ratio, 0.0, 1.0);
            }
        }

        public event Action? Tick;
        public event Action<SessionType>? SessionCompleted;
        public event Action? StateChanged;

        public PomodoroTimerService()
        {
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(250) // Frequent UI update interval, timestamp controls precision
            };
            _timer.Tick += OnTimerTick;

            ResetToMode(SessionType.Focus);
        }

        private TimeSpan GetRemainingTimeSpan()
        {
            if (State == TimerState.Running)
            {
                var remaining = _targetEndTimeUtc - DateTime.UtcNow;
                return remaining < TimeSpan.Zero ? TimeSpan.Zero : remaining;
            }
            return _remainingTimeSpan;
        }

        public void SetTestMode(bool enabled)
        {
            IsTestMode = enabled;
            ResetToMode(CurrentSessionType);
        }

        public void ResetToMode(SessionType type)
        {
            _timer.Stop();
            State = TimerState.Stopped;
            CurrentSessionType = type;

            var settings = DatabaseService.Instance.GetSettings();
            int minutes = type switch
            {
                SessionType.Focus => settings.FocusDurationMinutes,
                SessionType.ShortBreak => settings.ShortBreakMinutes,
                SessionType.LongBreak => settings.LongBreakMinutes,
                _ => 25
            };

            if (IsTestMode)
            {
                // Quick 10-second timer mode for verification testing
                _totalDurationTimeSpan = TimeSpan.FromSeconds(10);
            }
            else
            {
                _totalDurationTimeSpan = TimeSpan.FromMinutes(minutes);
            }

            _remainingTimeSpan = _totalDurationTimeSpan;

            Tick?.Invoke();
            StateChanged?.Invoke();
        }

        public void Start()
        {
            if (State == TimerState.Running) return;

            if (State == TimerState.Stopped)
            {
                _remainingTimeSpan = _totalDurationTimeSpan;
                _sessionStartTimeUtc = DateTime.UtcNow;
            }

            _targetEndTimeUtc = DateTime.UtcNow.Add(_remainingTimeSpan);
            State = TimerState.Running;
            _timer.Start();

            StateChanged?.Invoke();
        }

        public void Pause()
        {
            if (State != TimerState.Running) return;

            _timer.Stop();
            _remainingTimeSpan = GetRemainingTimeSpan();
            State = TimerState.Paused;

            StateChanged?.Invoke();
        }

        public void Reset()
        {
            ResetToMode(CurrentSessionType);
        }

        public void Skip()
        {
            AdvanceToNextSession(wasCompleted: false);
        }

        private void OnTimerTick(object? sender, EventArgs e)
        {
            if (State != TimerState.Running) return;

            var remaining = GetRemainingTimeSpan();
            Tick?.Invoke();

            if (remaining <= TimeSpan.Zero)
            {
                _timer.Stop();
                State = TimerState.Stopped;
                OnSessionFinished();
            }
        }

        private void OnSessionFinished()
        {
            var finishedType = CurrentSessionType;
            var settings = DatabaseService.Instance.GetSettings();

            if (finishedType == SessionType.Focus)
            {
                CompletedFocusSessionsCount++;

                // 1. Log session into SQLite database
                var sessionLog = new PomodoroSession
                {
                    TaskId = CurrentTask?.Id,
                    Type = SessionType.Focus,
                    DurationMinutes = (int)Math.Max(1, Math.Round(_totalDurationTimeSpan.TotalMinutes)),
                    StartedAt = _sessionStartTimeUtc,
                    CompletedAt = DateTime.UtcNow,
                    IsSuccessful = true
                };
                DatabaseService.Instance.RecordPomodoroSession(sessionLog);

                // 2. Increment active task Pomodoros count
                if (CurrentTask != null)
                {
                    DatabaseService.Instance.IncrementCompletedPomodoros(CurrentTask.Id);
                    CurrentTask.CompletedPomodoros++;
                }

                // 3. Audio & Desktop Notifications
                if (settings.NotificationsEnabled)
                {
                    bool isNextLongBreak = (CompletedFocusSessionsCount % settings.LongBreakInterval == 0);
                    string breakText = isNextLongBreak ? $"{settings.LongBreakMinutes}-minute Long Break" : $"{settings.ShortBreakMinutes}-minute Short Break";

                    NotificationService.Instance.ShowNotification(
                        "🎉 Focus Session Completed!",
                        $"Great work! Time for a {breakText}."
                    );
                }

                if (settings.SoundEnabled)
                {
                    SystemSounds.Asterisk.Play();
                }

                SessionCompleted?.Invoke(finishedType);

                // 4. Advance to Break
                AdvanceToNextSession(wasCompleted: true);
            }
            else
            {
                // Break Completed
                if (settings.NotificationsEnabled)
                {
                    NotificationService.Instance.ShowNotification(
                        "⚡ Break Time Ended!",
                        "Ready to jump into your next Focus session?"
                    );
                }

                if (settings.SoundEnabled)
                {
                    SystemSounds.Exclamation.Play();
                }

                SessionCompleted?.Invoke(finishedType);

                // Advance to Focus
                AdvanceToNextSession(wasCompleted: true);
            }
        }

        private void AdvanceToNextSession(bool wasCompleted)
        {
            var settings = DatabaseService.Instance.GetSettings();

            if (CurrentSessionType == SessionType.Focus)
            {
                // Check if Long Break threshold met
                if (CompletedFocusSessionsCount > 0 && CompletedFocusSessionsCount % settings.LongBreakInterval == 0)
                {
                    ResetToMode(SessionType.LongBreak);
                }
                else
                {
                    ResetToMode(SessionType.ShortBreak);
                }

                if (wasCompleted && settings.AutoStartBreaks)
                {
                    Start();
                }
            }
            else
            {
                // Break ended -> Next Focus
                ResetToMode(SessionType.Focus);

                if (wasCompleted && settings.AutoStartPomodoros)
                {
                    Start();
                }
            }
        }
    }
}
