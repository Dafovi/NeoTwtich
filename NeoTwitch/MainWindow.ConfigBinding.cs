using System.Windows;
using NeoTwitch.Models;
using NeoTwitch.Services;
using NeoTwitch.Services.Lights;
using NeoTwitch.Shared;
using NeoTwitch.ViewModels.Activity;
using NeoTwitch.ViewModels.Library;
using static NeoTwitch.Services.InputValueParser;

namespace NeoTwitch;

public partial class MainWindow
{
    private void LoadConfigIntoUi()
    {
        _loadingUi = true;

        try
        {
            ClientIdBox.Text = _config.TwitchClientId;
            ClientSecretBox.Text = _config.TwitchClientSecret;
            PortComboBox.SelectedValue = _config.SerialPort;
            PortComboBox.Text = _config.SerialPort;
            BaudRateBox.Text = _config.BaudRate.ToString();
            ArduinoEnabledCheck.IsChecked = _config.ArduinoEnabled;
            AutoTwitchCheck.IsChecked = _config.AutoConnectTwitch;
            AutoArduinoCheck.IsChecked = _config.AutoConnectArduino;
            StartHiddenCheck.IsChecked = _config.StartHidden;
            StartWithWindowsCheck.IsChecked = _config.StartWithWindows;
            ThemeModeBox.SelectedValue = _config.ThemeMode;
            CloseToTrayCheck.IsChecked = _config.CloseToTray;
            AlertVolumeSlider.Value = _config.AlertVolumePercent;
            VideoVolumeSlider.Value = _config.VideoVolumePercent;
            MaxQueuedSameRuleAlertsBox.Text = _config.MaxQueuedSameRuleAlerts.ToString();
            SameRuleQueueCooldownBox.Text = _config.SameRuleQueueCooldownMs.ToString();
            MaxQueuedDifferentRuleAlertsBox.Text = _config.MaxQueuedDifferentRuleAlerts.ToString();
            DifferentRuleQueueCooldownBox.Text = _config.DifferentRuleQueueCooldownMs.ToString();
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
            _rulesViewSource.Source = _config.Rules;
            RulesList.ItemsSource = _rulesViewSource.View;
            RuleAudioAssetBox.ItemsSource = _config.AudioLibrary;
            RuleAudioGroupBox.ItemsSource = _config.AudioGroups;
            NewAudioAlertBox.ItemsSource = AudioAlertChoices;
            NewAudioGroupBox.ItemsSource = AudioGroupChoices;
            RefreshObsSceneChoices();
            RefreshRulesView();
            StripsList.ItemsSource = _config.LedStrips;
            SettingsPathText.Text = _settingsStore.SettingsPath;
            BackupPathText.Text = $"Backups automaticos: {_settingsStore.BackupDirectory}";
            SettingsVersionText.Text = $"V{NeoTwitchProduct.CurrentVersionText}";
            UpdateCloseBehaviorCards();

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

        _config.TwitchClientId = ClientIdBox.Text.Trim();
        _config.TwitchClientSecret = ClientSecretBox.Text.Trim();
        _config.SerialPort = ParsePort(PortComboBox.SelectedValue as string ?? PortComboBox.Text);
        _config.BaudRate = ParseInt(BaudRateBox.Text, 115200, 300, 921600);
        _config.ArduinoEnabled = ArduinoEnabledCheck.IsChecked == true;
        _config.AutoConnectTwitch = AutoTwitchCheck.IsChecked == true;
        _config.AutoConnectArduino = AutoArduinoCheck.IsChecked == true;
        _config.StartHidden = StartHiddenCheck.IsChecked == true;
        _config.StartWithWindows = StartWithWindowsCheck.IsChecked == true;
        _config.ThemeMode = ThemeModeService.Normalize(ThemeModeBox.SelectedValue as string ?? _config.ThemeMode);
        _config.DarkMode = ThemeModeService.ResolveDarkMode(_config.ThemeMode);
        _config.CloseToTray = CloseToTrayCheck.IsChecked == true;
        _config.AlertVolumePercent = (int)Math.Round(AlertVolumeSlider.Value);
        _config.VideoVolumePercent = (int)Math.Round(VideoVolumeSlider.Value);
        _config.MaxQueuedSameRuleAlerts = ParseInt(MaxQueuedSameRuleAlertsBox.Text, 1, 0, 100);
        _config.SameRuleQueueCooldownMs = ParseInt(SameRuleQueueCooldownBox.Text, 0, 0, 600000);
        _config.MaxQueuedDifferentRuleAlerts = ParseInt(MaxQueuedDifferentRuleAlertsBox.Text, 3, 0, 100);
        _config.DifferentRuleQueueCooldownMs = ParseInt(DifferentRuleQueueCooldownBox.Text, 0, 0, 600000);
        _config.Alexa.Enabled = AlexaEnabledCheck.IsChecked == true;
        _config.Alexa.RelayUrl = AlexaRelayUrlBox.Text.Trim();
        _config.Alexa.AuthToken = AlexaAuthTokenBox.Text.Trim();
        _config.Obs.Enabled = ObsEnabledCheck.IsChecked == true;
        _config.Obs.Host = string.IsNullOrWhiteSpace(ObsHostBox.Text) ? "127.0.0.1" : ObsHostBox.Text.Trim();
        _config.Obs.Port = ParseInt(ObsPortBox.Text, 4455, 1, 65535);
        _config.Obs.Password = ObsPasswordBox.Text;
        _config.Obs.AutoReconnect = ObsAutoReconnectCheck.IsChecked == true;
        _config.Obs.OverlayWidth = ParseInt(ObsOverlayWidthBox.Text, 1920, 320, 7680);
        _config.Obs.OverlayHeight = ParseInt(ObsOverlayHeightBox.Text, 1080, 180, 4320);
        _config.Obs.OverlayMediaWidth = ParseInt(ObsOverlayMediaWidthBox.Text, 720, 32, 7680);
        _config.Obs.OverlayMediaHeight = ParseInt(ObsOverlayMediaHeightBox.Text, 420, 32, 4320);
        _config.Obs.OverlayPositionMode = ObsOverlayPositionBox.SelectedValue as string ?? "Center";
        _config.Obs.OverlayX = ParseInt(ObsOverlayXBox.Text, 0, 0, 7680);
        _config.Obs.OverlayY = ParseInt(ObsOverlayYBox.Text, 0, 0, 4320);
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

    private static void SetVisible(bool isVisible, params UIElement[] elements)
    {
        var visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
        foreach (var element in elements)
        {
            element.Visibility = visibility;
        }
    }

}
