using System.Windows;
using NeoTwitch.Models;
using NeoTwitch.Services;
using NeoTwitch.Services.Streaming;
using NeoTwitch.Services.Text;
using NeoTwitch.ViewModels.Activity;

namespace NeoTwitch;

public partial class MainWindow
{
    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        ApplyWindowChromeColor();
        MaximizeWindowToWorkArea();
        ConfigureActionIcons();
        AddLog(_text.Get(UiTextKeys.StartupReadyLog));
        AddLog(_text.Format(UiTextKeys.StartupSettingsPathLog, _settingsStore.SettingsPath));
        AddLog(_text.Format(UiTextKeys.StartupCrashLogPathLog, CrashReporter.PreferredLogPath));
        if (!string.IsNullOrWhiteSpace(_settingsStore.LastLoadError))
        {
            AddLog(_text.Format(UiTextKeys.StartupPreviousSettingsReadFailureLog, _settingsStore.LastLoadError));
        }

        if (_settingsStore.LastIntegrityWarnings.Count > 0)
        {
            AddLog(
                $"Configuración: {_settingsStore.LastIntegrityWarnings.Count} referencia(s) ambigua(s) se desactivaron de forma segura.",
                ActivityLogKind.Important);
        }

        if (_settingsStore.LastSecretFailures.Count > 0)
        {
            AddLog(
                $"Configuración: {_settingsStore.LastSecretFailures.Count} credencial(es) protegida(s) requieren autenticación de nuevo.",
                ActivityLogKind.Important);
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

        if (!_startupOptions.SuppressAutoConnect)
        {
            await AutoConnectArduinoAtStartupAsync();
            await AutoConnectTwitchAtStartupAsync();
            await AutoConnectYouTubeAtStartupAsync();
            await AutoConnectObsAtStartupAsync();
            InitializeAlexaRelayStatusAtStartup();
        }
    }

    private async Task AutoConnectArduinoAtStartupAsync()
    {
        if (!_config.ArduinoEnabled || !_config.AutoConnectArduino)
        {
            return;
        }

        if (!TryPrepareArduinoAutoConnectPort(out var selectedPort))
        {
            AddLog(_text.Get(UiTextKeys.StartupArduinoAutoConnectMissingPortLog), ActivityLogKind.Important);
            UpdateStatusText();
            return;
        }

        try
        {
            await ConnectArduinoAsync();
            await ApplyBackgroundAsync();
        }
        catch (Exception ex)
        {
            CrashReporter.Log(ex, _text.Format(UiTextKeys.StartupArduinoAutoConnectFailureCrash, selectedPort));
            AddLog(_text.Format(UiTextKeys.StartupArduinoAutoConnectFailureLog, selectedPort), ActivityLogKind.Important);
            UpdateStatusText();
        }
    }

    private async Task AutoConnectTwitchAtStartupAsync()
    {
        if (!_config.AutoConnectTwitch)
        {
            return;
        }

        if (!_config.Token.HasToken)
        {
            AddLog(_text.Get(UiTextKeys.StartupTwitchAutoConnectMissingTokenLog), ActivityLogKind.Twitch);
            UpdateStatusText();
            return;
        }

        try
        {
            await StartTwitchAsync();
        }
        catch (Exception ex)
        {
            AddLog(_text.Format(UiTextKeys.StartupTwitchAutoConnectFailureLog, ex.Message), ActivityLogKind.Important);
            UpdateStatusText();
        }
    }

    private async Task AutoConnectYouTubeAtStartupAsync()
    {
        var connection = StreamingPlatformConfigurationService.GetOrCreate(_config.StreamingPlatforms, StreamingPlatform.YouTube);
        if (!connection.IsEnabled || !_config.YouTubeToken.HasToken)
        {
            return;
        }

        try
        {
            await _youTubeAuthService.EnsureValidTokenAsync(_config, CancellationToken.None);
            if (!_config.YouTubeChannel.IsReady)
            {
                _config.YouTubeChannel = await _youTubeChannelService.GetCurrentChannelAsync(_config.YouTubeToken, CancellationToken.None);
                _connectionsViewModel.UpdateYouTubeChannel(_config.YouTubeChannel);
            }

            SaveConfig();
            _youTubeConnectionError = "";
            await _youTubePlatformProvider.StartAsync(CancellationToken.None);
            UpdateYouTubeConnectionUi();
        }
        catch (Exception ex)
        {
            _youTubeConnectionError = ex.Message;
            AddLog($"YouTube: no se pudo iniciar el lector de chat: {ex.Message}", ActivityLogKind.YouTube);
            UpdateYouTubeConnectionUi();
        }
    }

    private async Task AutoConnectObsAtStartupAsync()
    {
        if (!_config.Obs.Enabled || !_config.Obs.AutoReconnect)
        {
            return;
        }

        try
        {
            await ConnectObsAsync();
        }
        catch (Exception ex)
        {
            _obsConnectionError = ex.Message;
            AddLog(_text.Format(UiTextKeys.StartupObsAutoConnectFailureLog, ex.Message), ActivityLogKind.Important);
            UpdateObsStatusText();
        }
    }

    private void InitializeAlexaRelayStatusAtStartup()
    {
        if (!_config.Alexa.Enabled)
        {
            return;
        }

        if (_config.Alexa.IsConfigured)
        {
            AddLog(_text.Get(UiTextKeys.StartupAlexaRelayConfiguredLog), ActivityLogKind.Alexa);
        }

        UpdateAlexaStatusText();
    }
}
