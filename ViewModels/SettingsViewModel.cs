using System;
using System.IO;
using System.Windows.Input;
using Microsoft.Win32;
using FocusFlow.Data;
using FocusFlow.Models;
using FocusFlow.Services;

namespace FocusFlow.ViewModels
{
    public class SettingsViewModel : ViewModelBase
    {
        private const string StartupRegistryKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string AppName = "FocusFlow";

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
            ApplyStartupSetting(Settings.LaunchAtStartup);
            NotificationService.Instance.ShowNotification("Settings Saved", "Your FocusFlow preferences have been updated.");
        }

        private void ResetDefaults()
        {
            Settings = new UserSettings();
            DatabaseService.Instance.SaveSettings(Settings);
            ThemeService.Instance.ApplyTheme(Settings.Theme);
            PomodoroTimerService.Instance.ResetToMode(SessionType.Focus);
            ApplyStartupSetting(Settings.LaunchAtStartup);
            NotificationService.Instance.ShowNotification("Settings Reset", "Restored default settings.");
        }

        public static void ApplyStartupSetting(bool enableStartup)
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(StartupRegistryKey, true);
                if (key == null) return;

                if (enableStartup)
                {
                    string? exePath = Environment.ProcessPath;
                    if (!string.IsNullOrEmpty(exePath) && File.Exists(exePath))
                    {
                        key.SetValue(AppName, $"\"{exePath}\"");
                    }
                }
                else
                {
                    if (key.GetValue(AppName) != null)
                    {
                        key.DeleteValue(AppName, false);
                    }
                }
            }
            catch (Exception ex)
            {
                App.LogLifecycle($"ApplyStartupSetting error: {ex.Message}");
            }
        }
    }
}
