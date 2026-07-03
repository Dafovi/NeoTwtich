using NeoTwitch.Models;
using NeoTwitch.Services;
using NeoTwitch.Services.Configuration;
using NeoTwitch.Services.Lights;
using NeoTwitch.Shared;
using NeoTwitch.ViewModels.Activity;
using NeoTwitch.ViewModels.Library;

namespace NeoTwitch;

public partial class MainWindow
{
    private void LoadConfigIntoUi()
    {
        _loadingUi = true;

        try
        {
            LoadConnectionConfigIntoUi();
            LoadGlobalPreferencesIntoUi();
            LoadBackgroundConfigIntoUi();
            BindConfigCollectionsIntoUi();
            RefreshObsSceneChoices();
            RefreshRulesView();
            LoadSettingsMetadataIntoUi();

            if (_config.Rules.Count > 0)
            {
                _alertsViewModel.SelectedRule = _alertsViewModel.FirstVisibleRule() ?? _config.Rules[0];
            }

            if (_config.LedStrips.Count > 0)
            {
                StripsList.SelectedIndex = 0;
            }

            LoadSelectedRuleIntoUi();
            LoadSelectedStripIntoUi();
            RefreshLoadedConfigUi();
        }
        finally
        {
            _loadingUi = false;
        }
    }

    private void SaveGlobalSettingsFromFields()
    {
        if (_loadingUi)
        {
            return;
        }

        GlobalSettingsFormService.Apply(
            _config,
            new GlobalSettingsFormValues(
                _connectionsViewModel.TwitchClientId,
                _connectionsViewModel.TwitchClientSecret,
                _connectionsViewModel.SerialPort,
                _connectionsViewModel.BaudRateText,
                _connectionsViewModel.ArduinoEnabled,
                _settingsViewModel.AutoConnectTwitch,
                _settingsViewModel.AutoConnectArduino,
                _settingsViewModel.StartHidden,
                _settingsViewModel.StartWithWindows,
                _settingsViewModel.ThemeMode,
                _settingsViewModel.CloseToTray,
                _audioLibraryViewModel.VolumePercent,
                _videoLibraryViewModel.VolumePercent,
                _settingsViewModel.MaxQueuedSameRuleAlertsText,
                _settingsViewModel.SameRuleQueueCooldownText,
                _settingsViewModel.MaxQueuedDifferentRuleAlertsText,
                _settingsViewModel.DifferentRuleQueueCooldownText,
                _connectionsViewModel.AlexaEnabled,
                _connectionsViewModel.AlexaRelayUrl,
                _connectionsViewModel.AlexaAuthToken,
                _connectionsViewModel.ObsEnabled,
                _connectionsViewModel.ObsHost,
                _connectionsViewModel.ObsPortText,
                _connectionsViewModel.ObsPassword,
                _settingsViewModel.ObsAutoReconnect,
                _obsViewModel.OverlayWidthText,
                _obsViewModel.OverlayHeightText,
                _obsViewModel.OverlayMediaWidthText,
                _obsViewModel.OverlayMediaHeightText,
                _obsViewModel.OverlayPositionMode,
                _obsViewModel.OverlayXText,
                _obsViewModel.OverlayYText));
    }

    private void ApplyStartWithWindowsRegistration()
    {
        if (_lastAppliedStartWithWindows == _config.StartWithWindows)
        {
            return;
        }

        try
        {
            _windowsStartupService.SetEnabled(_config.StartWithWindows);
            _lastAppliedStartWithWindows = _config.StartWithWindows;
        }
        catch (Exception ex)
        {
            AddLog($"Inicio con Windows: {ex.Message}", ActivityLogKind.Important);
        }
    }

}
