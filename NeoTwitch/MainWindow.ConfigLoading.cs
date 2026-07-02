using NeoTwitch.Services;
using NeoTwitch.Models;
using NeoTwitch.Services.Text;
using NeoTwitch.Shared;
using NeoTwitch.ViewModels.Library;

namespace NeoTwitch;

public partial class MainWindow
{
    private void LoadConnectionConfigIntoUi()
    {
        ClientIdBox.Text = _config.TwitchClientId;
        ClientSecretBox.Text = _config.TwitchClientSecret;
        PortComboBox.SelectedValue = _config.SerialPort;
        PortComboBox.Text = _config.SerialPort;
        BaudRateBox.Text = _config.BaudRate.ToString();
        ArduinoEnabledCheck.IsChecked = _config.ArduinoEnabled;
        AlexaEnabledCheck.IsChecked = _config.Alexa.Enabled;
        AlexaRelayUrlBox.Text = _config.Alexa.RelayUrl;
        AlexaAuthTokenBox.Text = _config.Alexa.AuthToken;
        ObsEnabledCheck.IsChecked = _config.Obs.Enabled;
        ObsHostBox.Text = _config.Obs.Host;
        ObsPortBox.Text = _config.Obs.Port.ToString();
        ObsPasswordBox.Text = _config.Obs.Password;
        ObsAutoReconnectCheck.IsChecked = _config.Obs.AutoReconnect;
        ObsOverlayWidthBox.Text = _config.Obs.OverlayWidth.ToString();
        ObsOverlayHeightBox.Text = _config.Obs.OverlayHeight.ToString();
        ObsOverlayMediaWidthBox.Text = _config.Obs.OverlayMediaWidth.ToString();
        ObsOverlayMediaHeightBox.Text = _config.Obs.OverlayMediaHeight.ToString();
        ObsOverlayPositionBox.SelectedValue = _config.Obs.OverlayPositionMode;
        ObsOverlayXBox.Text = _config.Obs.OverlayX.ToString();
        ObsOverlayYBox.Text = _config.Obs.OverlayY.ToString();
    }

    private void LoadGlobalPreferencesIntoUi()
    {
        AutoTwitchCheck.IsChecked = _config.AutoConnectTwitch;
        AutoArduinoCheck.IsChecked = _config.AutoConnectArduino;
        StartHiddenCheck.IsChecked = _config.StartHidden;
        StartWithWindowsCheck.IsChecked = _config.StartWithWindows;
        ThemeModeBox.SelectedValue = _config.ThemeMode;
        CloseToTrayCheck.IsChecked = _config.CloseToTray;
        _audioLibraryViewModel.SetVolume(_config.AlertVolumePercent, notify: false);
        _videoLibraryViewModel.SetVolume(_config.VideoVolumePercent, notify: false);
    }

    private void LoadQueueConfigIntoUi()
    {
        MaxQueuedSameRuleAlertsBox.Text = _config.MaxQueuedSameRuleAlerts.ToString();
        SameRuleQueueCooldownBox.Text = _config.SameRuleQueueCooldownMs.ToString();
        MaxQueuedDifferentRuleAlertsBox.Text = _config.MaxQueuedDifferentRuleAlerts.ToString();
        DifferentRuleQueueCooldownBox.Text = _config.DifferentRuleQueueCooldownMs.ToString();
    }

    private void LoadBackgroundConfigIntoUi()
    {
        _lightsViewModel.LoadBackground(_config);
        BackgroundAlexaEnabledCheck.IsChecked = _config.BackgroundAlexaEnabled;
        BackgroundAlexaTurnOffAfterEventCheck.IsChecked = _config.BackgroundAlexaTurnOffAfterEvent;
        BackgroundAlexaOnEventBox.Text = _config.BackgroundAlexaOnEventName;
        BackgroundAlexaOffEventBox.Text = _config.BackgroundAlexaOffEventName;
    }

    private void BindConfigCollectionsIntoUi()
    {
        _alertsViewModel.SetRulesSource(_config.Rules);
        _alertsViewModel.UpdateEditorChoices(
            _eventOptions,
            _patternOptions,
            _config.AudioLibrary,
            _config.AudioGroups,
            _obsSceneChoices,
            _obsMediaKindOptions,
            _mediaSourceModeOptions);
        _audioLibraryViewModel.SetNewAssetChoices(AudioGroupChoices, AudioAlertChoices);
        _imageLibraryViewModel.SetNewAssetChoices(ImageGroupChoices);
        _videoLibraryViewModel.SetNewAssetChoices(VideoGroupChoices);
        _lightsViewModel.SetLedStripsSource(_config.LedStrips);
    }

    private void LoadSettingsMetadataIntoUi()
    {
        _settingsViewModel.UpdateMetadata(
            _settingsStore.SettingsPath,
            _text.Format(UiTextKeys.SettingsAutomaticBackupsText, _settingsStore.BackupDirectory),
            $"V{NeoTwitchProduct.CurrentVersionText}");
        UpdateCloseBehaviorCards();
    }

    private void RefreshLoadedConfigUi()
    {
        UpdateBackgroundOptionVisibility();
        UpdateBackgroundPatternTileSelection();
        UpdateBackgroundLedPreviewFrame();
        RefreshAudioLibraryView();
        UpdateAudioFilterButtons();
        UpdateLightsArduinoStatus();
        ApplyBackgroundOutputMode();
        UpdateAlexaStatusText();
        UpdateObsStatusText();
        UpdateSensitiveFieldVisibility();
        ApplyTheme();
        UpdateNavigationButtons();
        UpdateStatusText();
        RefreshMediaLibraryView(MediaLibraryKind.Image);
        RefreshMediaLibraryView(MediaLibraryKind.Video);
    }
}
