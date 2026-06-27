using System.Windows;
using NeoTwitch.Services;
using NeoTwitch.Services.Text;
using NeoTwitch.ViewModels.Activity;

namespace NeoTwitch;

public partial class MainWindow
{
    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        ApplyWindowChromeColor();
        ConfigureActionIcons();
        AddLog(_text.Get(UiTextKeys.StartupReadyLog));
        AddLog(_text.Format(UiTextKeys.StartupSettingsPathLog, _settingsStore.SettingsPath));
        AddLog(_text.Format(UiTextKeys.StartupCrashLogPathLog, CrashReporter.PreferredLogPath));
        if (!string.IsNullOrWhiteSpace(_settingsStore.LastLoadError))
        {
            AddLog(_text.Format(UiTextKeys.StartupPreviousSettingsReadFailureLog, _settingsStore.LastLoadError));
        }

        ApplyStartWithWindowsRegistration();
        _ = CheckForUpdatesAsync();

        if (_startupOptions.DebugMode)
        {
            AddLog(_text.Get(UiTextKeys.StartupDebugModeLog));
        }

        if (_startupOptions.SuppressAutoConnect)
        {
            AddLog(_text.Get(UiTextKeys.StartupAutoConnectSuppressedLog), ActivityLogKind.Important);
        }

        if (_config.StartHidden && !_startupOptions.SuppressStartHidden)
        {
            Hide();
        }

        if (!_startupOptions.SuppressAutoConnect && _config.ArduinoEnabled && _config.AutoConnectArduino && !string.IsNullOrWhiteSpace(_config.SerialPort))
        {
            try
            {
                await ConnectArduinoAsync();
                await ApplyBackgroundAsync();
            }
            catch (Exception ex)
            {
                CrashReporter.Log(ex, _text.Format(UiTextKeys.StartupArduinoAutoConnectFailureCrash, _config.SerialPort));
                AddLog(_text.Format(UiTextKeys.StartupArduinoAutoConnectFailureLog, _config.SerialPort), ActivityLogKind.Important);
                UpdateStatusText();
            }
        }

        if (!_startupOptions.SuppressAutoConnect && _config.AutoConnectTwitch && _config.Token.HasToken)
        {
            try
            {
                await StartTwitchAsync();
            }
            catch (Exception ex)
            {
                AddLog($"Twitch: {ex.Message}");
            }
        }

        if (!_startupOptions.SuppressAutoConnect && _config.Obs.Enabled && _config.Obs.AutoReconnect)
        {
            try
            {
                await ConnectObsAsync();
            }
            catch (Exception ex)
            {
                AddLog($"OBS: {ex.Message}", ActivityLogKind.Important);
            }
        }
    }
}
