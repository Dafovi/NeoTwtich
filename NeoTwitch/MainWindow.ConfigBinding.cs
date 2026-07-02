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
            LoadQueueConfigIntoUi();
            LoadBackgroundConfigIntoUi();
            BindConfigCollectionsIntoUi();
            RefreshObsSceneChoices();
            RefreshRulesView();
            LoadSettingsMetadataIntoUi();

            if (_config.Rules.Count > 0)
            {
                RulesList.SelectedIndex = 0;
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
                PortComboBox.SelectedValue as string ?? PortComboBox.Text,
                BaudRateBox.Text,
                ArduinoEnabledCheck.IsChecked == true,
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
                AlexaEnabledCheck.IsChecked == true,
                AlexaRelayUrlBox.Text,
                AlexaAuthTokenBox.Text,
                ObsEnabledCheck.IsChecked == true,
                ObsHostBox.Text,
                ObsPortBox.Text,
                ObsPasswordBox.Text,
                _settingsViewModel.ObsAutoReconnect,
                ObsOverlayWidthBox.Text,
                ObsOverlayHeightBox.Text,
                ObsOverlayMediaWidthBox.Text,
                ObsOverlayMediaHeightBox.Text,
                ObsOverlayPositionBox.SelectedValue as string ?? "Center",
                ObsOverlayXBox.Text,
                ObsOverlayYBox.Text));
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
