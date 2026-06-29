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
        AlertVolumeSlider.Value = _config.AlertVolumePercent;
        VideoVolumeSlider.Value = _config.VideoVolumePercent;
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
        BackgroundEnabledCheck.IsChecked = _config.BackgroundEnabled;
        BackgroundAlexaEnabledCheck.IsChecked = _config.BackgroundAlexaEnabled;
        BackgroundAlexaTurnOffAfterEventCheck.IsChecked = _config.BackgroundAlexaTurnOffAfterEvent;
        BackgroundAlexaOnEventBox.Text = _config.BackgroundAlexaOnEventName;
        BackgroundAlexaOffEventBox.Text = _config.BackgroundAlexaOffEventName;
        BackgroundPinsBox.Text = _config.BackgroundTargetPins;
        BackgroundPatternBox.SelectedValue = _config.BackgroundPattern;
        BackgroundPrimaryColorBox.Text = LightCommand.NormalizeColor(_config.BackgroundPrimaryColor);
        BackgroundSecondaryColorBox.Text = LightCommand.NormalizeColor(_config.BackgroundSecondaryColor);
        BackgroundTertiaryColorBox.Text = LightCommand.NormalizeColor(_config.BackgroundTertiaryColor);
        BackgroundBrightnessSlider.Value = _config.BackgroundBrightness;
        BackgroundCycleSlider.Value = _config.BackgroundCycleMs;
        BackgroundStepSlider.Value = _config.BackgroundStepMs;
    }

    private void BindConfigCollectionsIntoUi()
    {
        _alertsViewModel.SetRulesSource(_config.Rules);
        RuleAudioAssetBox.ItemsSource = _config.AudioLibrary;
        RuleAudioGroupBox.ItemsSource = _config.AudioGroups;
        NewAudioAlertBox.ItemsSource = AudioAlertChoices;
        NewAudioGroupBox.ItemsSource = AudioGroupChoices;
        StripsList.ItemsSource = _config.LedStrips;
    }

    private void LoadSettingsMetadataIntoUi()
    {
        SettingsPathText.Text = _settingsStore.SettingsPath;
        BackupPathText.Text = _text.Format(UiTextKeys.SettingsAutomaticBackupsText, _settingsStore.BackupDirectory);
        SettingsVersionText.Text = $"V{NeoTwitchProduct.CurrentVersionText}";
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
        UpdateVideoVolumeText();
    }
}
