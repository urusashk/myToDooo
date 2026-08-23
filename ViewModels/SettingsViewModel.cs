using System.Windows.Input;
using FocusFlow.Data;
using FocusFlow.Models;
using FocusFlow.Services;

namespace FocusFlow.ViewModels
{
    public class SettingsViewModel : ViewModelBase
    {
        private UserSettings _settings;

        public UserSettings Settings
        {
            get => _settings;
            set => SetProperty(ref _settings, value);
        }

        public ICommand SaveSettingsCommand { get; }
        public ICommand ResetDefaultsCommand { get; }
        public ICommand ToggleThemeCommand { get; }

        public SettingsViewModel()
        {
            _settings = DatabaseService.Instance.GetSettings();
            SaveSettingsCommand = new RelayCommand(SaveSettings);
            ResetDefaultsCommand = new RelayCommand(ResetDefaults);
            ToggleThemeCommand = new RelayCommand(ToggleTheme);

            ThemeService.Instance.ApplyTheme(_settings.Theme);
        }

        public override void OnNavigatedTo()
        {
            Settings = DatabaseService.Instance.GetSettings();
        }

        private void ToggleTheme(object? parameter)
        {
            if (parameter is string themeName)
            {
                Settings.Theme = themeName;
                ThemeService.Instance.ApplyTheme(themeName);
                DatabaseService.Instance.SaveSettings(Settings);
            }
        }

        private void SaveSettings()
        {
            DatabaseService.Instance.SaveSettings(Settings);
            ThemeService.Instance.ApplyTheme(Settings.Theme);
            PomodoroTimerService.Instance.ResetToMode(PomodoroTimerService.Instance.CurrentSessionType);
            NotificationService.Instance.ShowNotification("Settings Saved", "Your FocusFlow preferences have been updated.");
        }

        private void ResetDefaults()
        {
            Settings = new UserSettings();
            DatabaseService.Instance.SaveSettings(Settings);
            ThemeService.Instance.ApplyTheme(Settings.Theme);
            PomodoroTimerService.Instance.ResetToMode(SessionType.Focus);
            NotificationService.Instance.ShowNotification("Settings Reset", "Restored default settings.");
        }
    }
}
