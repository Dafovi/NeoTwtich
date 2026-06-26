using System.Windows;
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

        GlobalSettingsFormService.Apply(
            _config,
            new GlobalSettingsFormValues(
                ClientIdBox.Text,
                ClientSecretBox.Text,
                PortComboBox.SelectedValue as string ?? PortComboBox.Text,
                BaudRateBox.Text,
                ArduinoEnabledCheck.IsChecked == true,
                AutoTwitchCheck.IsChecked == true,
                AutoArduinoCheck.IsChecked == true,
                StartHiddenCheck.IsChecked == true,
                StartWithWindowsCheck.IsChecked == true,
                ThemeModeBox.SelectedValue as string ?? _config.ThemeMode,
                CloseToTrayCheck.IsChecked == true,
                AlertVolumeSlider.Value,
                VideoVolumeSlider.Value,
                MaxQueuedSameRuleAlertsBox.Text,
                SameRuleQueueCooldownBox.Text,
                MaxQueuedDifferentRuleAlertsBox.Text,
                DifferentRuleQueueCooldownBox.Text,
                AlexaEnabledCheck.IsChecked == true,
                AlexaRelayUrlBox.Text,
                AlexaAuthTokenBox.Text,
                ObsEnabledCheck.IsChecked == true,
                ObsHostBox.Text,
                ObsPortBox.Text,
                ObsPasswordBox.Text,
                ObsAutoReconnectCheck.IsChecked == true,
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

    private static void SetVisible(bool isVisible, params UIElement[] elements)
    {
        var visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
        foreach (var element in elements)
        {
            element.Visibility = visibility;
        }
    }

}
