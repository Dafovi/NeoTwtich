using System.Collections.ObjectModel;
using System.Windows;
using NeoTwitch.Models;
using NeoTwitch.Services;
using NeoTwitch.Services.Lights;
using NeoTwitch.Shared;
using NeoTwitch.ViewModels.Activity;
using NeoTwitch.ViewModels.Library;
using static NeoTwitch.Services.InputValueParser;
using static NeoTwitch.Services.Text.UiTextFormatter;
using static NeoTwitch.Services.Ui.UiBrushFactory;

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

    private void LoadSelectedRuleIntoUi()
    {
        _loadingRule = true;

        try
        {
            RuleEditorPanel.IsEnabled = RulesList.SelectedItem is EventRule;

            if (RulesList.SelectedItem is not EventRule rule)
            {
                _editingRule = null;
                _loadedRuleSnapshot = null;
                SetRuleDirtyState(false);
                return;
            }

            _editingRule = rule;
            RuleEnabledCheck.IsChecked = rule.IsEnabled;
            RuleNameBox.Text = rule.Name;
            EventKindBox.SelectedValue = rule.EventKind;
            UpdateEventKindTileSelection();
            RewardTitleBox.Text = rule.CustomRewardTitle;
            ChatCommandBox.Text = rule.ChatCommand;
            MinimumBitsBox.Text = rule.MinimumBits.ToString();
            ChatMessageCheck.IsChecked = rule.SendChatMessage;
            ChatMessageBox.Text = rule.ChatMessageTemplate;
            AlexaEventCheck.IsChecked = rule.SendAlexaEvent;
            ObsSceneCheck.IsChecked = rule.SendObsScene;
            RuleObsSceneBox.SelectedValue = rule.ObsSceneName;
            ObsSceneDelayBox.Text = rule.ObsSceneDelayMs.ToString();
            ObsReturnCheck.IsChecked = rule.ObsReturnToPreviousScene;
            ObsReturnDelayBox.Text = rule.ObsReturnDelayMs.ToString();
            ObsMediaCheck.IsChecked = rule.SendObsMedia;
            RuleObsMediaKindBox.SelectedValue = rule.ObsMediaKind;
            RuleObsMediaSourceModeBox.SelectedValue = rule.ObsMediaSourceMode;
            RefreshRuleObsMediaChoices();
            RuleObsMediaAssetBox.SelectedValue = rule.ObsMediaAssetId;
            RuleObsMediaGroupBox.SelectedValue = rule.ObsMediaGroupId;
            ObsMediaDurationBox.Text = rule.ObsMediaDurationMs.ToString();
            UseLightsCheck.IsChecked = rule.UseLights;
            PlayAudioCheck.IsChecked = rule.PlayAudio;
            _ruleAudioMode = rule.AudioSourceMode;
            RuleAudioAssetBox.SelectedValue = rule.AudioAssetId;
            RuleAudioGroupBox.SelectedValue = rule.AudioGroupId;
            PatternBox.SelectedValue = rule.Pattern;
            TargetPinsBox.Text = rule.TargetPins;
            RefreshRulePinChoices();
            PrimaryColorBox.Text = LightCommand.NormalizeColor(rule.PrimaryColor);
            SecondaryColorBox.Text = LightCommand.NormalizeColor(rule.SecondaryColor);
            TertiaryColorBox.Text = LightCommand.NormalizeColor(rule.TertiaryColor);
            BrightnessSlider.Value = rule.Brightness;
            DurationSlider.Value = rule.DurationMs;
            CycleSlider.Value = rule.CycleMs;
            StepSlider.Value = rule.StepMs;
            UpdateColorButtons();
            UpdateSliderLabels();
            UpdatePatternTileSelection();
            UpdateRuleObsMediaModeSelection();
            UpdateRuleLedPreviewFrame();
            CaptureCurrentRuleSnapshot();
            SetRuleDirtyState(false);
        }
        finally
        {
            _loadingRule = false;
            UpdateRuleOptionVisibility();
            UpdateRuleLedPreviewTimerState();
        }
    }

    private void LoadSelectedStripIntoUi()
    {
        _loadingStrip = true;

        try
        {
            StripEditorPanel.IsEnabled = StripsList.SelectedItem is LedStripConfig;

            if (StripsList.SelectedItem is not LedStripConfig strip)
            {
                return;
            }

            StripNameBox.Text = strip.Name;
            StripPinBox.Text = strip.Pin.ToString();
            StripLedCountBox.Text = strip.LedCount.ToString();
        }
        finally
        {
            _loadingStrip = false;
            UpdateLightsArduinoStatus();
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

    private bool SaveCurrentRuleFromFields()
    {
        if (_loadingRule
            || _editingRule is not EventRule rule
            || RulesList.SelectedItem is not EventRule selectedRule
            || !ReferenceEquals(selectedRule, rule)
            || !_config.Rules.Contains(rule)
            || EventKindBox.SelectedValue is not TwitchEventKind kind
            || !Enum.IsDefined(kind))
        {
            return false;
        }

        var ruleName = RuleNameBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(ruleName))
        {
            ruleName = string.IsNullOrWhiteSpace(rule.Name)
                ? DisplayNames.For(kind)
                : rule.Name;
        }

        rule.IsEnabled = RuleEnabledCheck.IsChecked == true;
        rule.Name = ruleName;
        rule.EventKind = kind;
        rule.CustomRewardTitle = RewardTitleBox.Text.Trim();
        rule.ChatCommand = ChatCommandBox.Text.Trim();
        rule.MinimumBits = ParseInt(MinimumBitsBox.Text, 1, 1, 1_000_000);
        rule.SendChatMessage = ChatMessageCheck.IsChecked == true;
        rule.ChatMessageTemplate = ChatMessageBox.Text.Trim();
        rule.SendAlexaEvent = AlexaEventCheck.IsChecked == true;
        rule.SendObsScene = ObsSceneCheck.IsChecked == true;
        rule.ObsSceneName = RuleObsSceneBox.SelectedValue as string ?? RuleObsSceneBox.Text.Trim();
        rule.ObsSceneDelayMs = ParseInt(ObsSceneDelayBox.Text, 0, 0, 600000);
        rule.ObsReturnToPreviousScene = ObsReturnCheck.IsChecked == true;
        rule.ObsReturnDelayMs = ParseInt(ObsReturnDelayBox.Text, 15000, 0, 600000);
        rule.SendObsMedia = ObsMediaCheck.IsChecked == true;
        rule.ObsMediaKind = RuleObsMediaKindBox.SelectedValue is ObsMediaKind mediaKind
            ? mediaKind
            : ObsMediaKind.Image;
        rule.ObsMediaSourceMode = RuleObsMediaSourceModeBox.SelectedValue is MediaSourceMode mediaSourceMode
            ? mediaSourceMode
            : MediaSourceMode.Single;
        RefreshRuleObsMediaChoices();
        rule.ObsMediaAssetId = RuleObsMediaAssetBox.SelectedValue as string ?? "";
        rule.ObsMediaGroupId = RuleObsMediaGroupBox.SelectedValue as string ?? "";
        rule.ObsMediaDurationMs = ParseInt(ObsMediaDurationBox.Text, 5000, 250, 600000);
        rule.UseLights = UseLightsCheck.IsChecked == true;
        rule.PlayAudio = PlayAudioCheck.IsChecked == true;
        rule.AudioSourceMode = _ruleAudioMode;
        rule.AudioAssetId = RuleAudioAssetBox.SelectedValue as string ?? "";
        rule.AudioGroupId = RuleAudioGroupBox.SelectedValue as string ?? "";
        rule.AudioPath = rule.AudioSourceMode == AudioSourceMode.Single
            ? _config.AudioLibrary.FirstOrDefault(audio => string.Equals(audio.Id, rule.AudioAssetId, StringComparison.OrdinalIgnoreCase))?.FilePath ?? ""
            : "";
        rule.Pattern = PatternBox.SelectedValue is LightPattern pattern ? pattern : LightPattern.Pulse;
        rule.TargetPins = string.Join(", ", LightCommand.ParsePins(TargetPinsBox.Text));
        rule.PrimaryColor = LightCommand.NormalizeColor(PrimaryColorBox.Text);
        rule.SecondaryColor = LightCommand.NormalizeColor(SecondaryColorBox.Text);
        rule.TertiaryColor = LightCommand.NormalizeColor(TertiaryColorBox.Text);
        rule.Brightness = (int)Math.Round(BrightnessSlider.Value);
        rule.DurationMs = (int)Math.Round(DurationSlider.Value);
        rule.CycleMs = (int)Math.Round(CycleSlider.Value);
        rule.StepMs = (int)Math.Round(StepSlider.Value);

        UpdateColorButtons();
        UpdateSliderLabels();
        UpdatePatternTileSelection();
        UpdateRuleAudioModeSelection();
        UpdateRuleObsMediaModeSelection();
        UpdateRuleLedPreviewFrame();
        UpdateRuleOptionVisibility();
        UpdateRuleLedPreviewTimerState();
        RefreshRulesView();
        RefreshAudioLibraryView();

        return true;
    }

    private void SaveBackgroundFromFields()
    {
        _config.BackgroundEnabled = BackgroundEnabledCheck.IsChecked == true;
        _config.BackgroundAlexaEnabled = BackgroundAlexaEnabledCheck.IsChecked == true;
        _config.BackgroundAlexaTurnOffAfterEvent = BackgroundAlexaTurnOffAfterEventCheck.IsChecked == true;
        _config.BackgroundAlexaOnEventName = NormalizeEventName(BackgroundAlexaOnEventBox.Text, "luz_encendida");
        _config.BackgroundAlexaOffEventName = NormalizeEventName(BackgroundAlexaOffEventBox.Text, "luz_apagada");
        _config.BackgroundTargetPins = string.Join(", ", LightCommand.ParsePins(BackgroundPinsBox.Text));
        _config.BackgroundPattern = BackgroundPatternBox.SelectedValue is LightPattern pattern ? pattern : LightPattern.Solid;
        _config.BackgroundPrimaryColor = LightCommand.NormalizeColor(BackgroundPrimaryColorBox.Text);
        _config.BackgroundSecondaryColor = LightCommand.NormalizeColor(BackgroundSecondaryColorBox.Text);
        _config.BackgroundTertiaryColor = LightCommand.NormalizeColor(BackgroundTertiaryColorBox.Text);
        _config.BackgroundBrightness = (int)Math.Round(BackgroundBrightnessSlider.Value);
        _config.BackgroundCycleMs = (int)Math.Round(BackgroundCycleSlider.Value);
        _config.BackgroundStepMs = (int)Math.Round(BackgroundStepSlider.Value);

        UpdateColorButtons();
        UpdateSliderLabels();
        UpdateBackgroundPatternTileSelection();
        UpdateBackgroundLedPreviewFrame();
        UpdateBackgroundOptionVisibility();
        UpdateAlexaStatusText();
    }

    private void SaveCurrentStripFromFields()
    {
        if (_loadingStrip || StripsList.SelectedItem is not LedStripConfig strip)
        {
            return;
        }

        strip.Name = string.IsNullOrWhiteSpace(StripNameBox.Text)
            ? "Tira LED"
            : StripNameBox.Text.Trim();
        strip.Pin = ParseInt(StripPinBox.Text, 6, 0, 53);
        strip.LedCount = ParseInt(StripLedCountBox.Text, 30, 1, 600);

        StripsList.Items.Refresh();
        RefreshRulesView();
        UpdateLightsArduinoStatus();
        RefreshRulePinChoices();
    }

    private void UpdateRuleOptionVisibility()
    {
        var kind = EventKindBox.SelectedValue is TwitchEventKind eventKind
            ? eventKind
            : TwitchEventKind.Follow;
        var arduinoAvailable = _config.ArduinoEnabled;
        var useLights = arduinoAvailable && UseLightsCheck.IsChecked == true;
        var playAudio = PlayAudioCheck.IsChecked == true;
        var sendChat = ChatMessageCheck.IsChecked == true;
        var alexaAvailable = _config.Alexa.IsConfigured;
        var sendAlexa = AlexaEventCheck.IsChecked == true;
        var obsAvailable = _config.Obs.IsConfigured;
        var sendObsScene = ObsSceneCheck.IsChecked == true;
        var selectedObsSceneName = RuleObsSceneBox.SelectedValue as string ?? "";
        var returnObsScene = ObsReturnCheck.IsChecked == true;
        var sendObsMedia = ObsMediaCheck.IsChecked == true;
        var obsMediaKind = RuleObsMediaKindBox.SelectedValue is ObsMediaKind selectedObsMediaKind
            ? selectedObsMediaKind
            : ObsMediaKind.Image;
        var obsMediaSourceMode = RuleObsMediaSourceModeBox.SelectedValue is MediaSourceMode selectedObsMediaSourceMode
            ? selectedObsMediaSourceMode
            : MediaSourceMode.Single;
        var obsMediaHasAssets = (obsMediaKind == ObsMediaKind.Image ? _config.ImageLibrary.Count : _config.VideoLibrary.Count) > 0;
        var obsMediaHasGroups = (obsMediaKind == ObsMediaKind.Image ? _config.ImageGroups.Count : _config.VideoGroups.Count) > 0;
        var pattern = PatternBox.SelectedValue is LightPattern selectedPattern
            ? selectedPattern
            : LightPattern.Pulse;

        SetVisible(kind == TwitchEventKind.ChannelPointRedemption, RewardTitleLabel, RewardTitleBox);
        SetVisible(kind == TwitchEventKind.ChatCommand, ChatCommandLabel, ChatCommandBox);
        SetVisible(kind == TwitchEventKind.Cheer, MinimumBitsLabel, MinimumBitsBox);
        var hasAudios = _config.AudioLibrary.Count > 0;
        var hasGroups = _config.AudioGroups.Count > 0;
        SetVisible(playAudio, AudioDetailsPanel, AudioLabel, AudioPanel);
        SetVisible(playAudio && _ruleAudioMode == AudioSourceMode.Single && hasAudios, RuleAudioSinglePanel);
        SetVisible(playAudio && _ruleAudioMode == AudioSourceMode.Group && hasGroups, RuleAudioGroupPanel);
        SetVisible(playAudio && ((_ruleAudioMode == AudioSourceMode.Single && !hasAudios) || (_ruleAudioMode == AudioSourceMode.Group && !hasGroups)), RuleAudioEmptyHintText);
        SetVisible(sendChat, ChatDetailsPanel, ChatMessageLabel, ChatMessageBox);
        SetVisible(arduinoAvailable, UseLightsActionCard);
        SetVisible(alexaAvailable, AlexaActionCard);
        SetVisible(alexaAvailable && sendAlexa, AlexaDetailsPanel, AlexaRuleHintText);
        SetVisible(obsAvailable, ObsActionCard);
        SetVisible(obsAvailable, ObsDetailsPanel);
        SetVisible(obsAvailable && sendObsScene, ObsSceneDetailsPanel);
        SetVisible(obsAvailable && sendObsScene && !string.IsNullOrWhiteSpace(selectedObsSceneName), ObsSceneTimingGrid);
        SetVisible(obsAvailable && sendObsScene && !sendObsMedia && returnObsScene, ObsReturnDelayPanel);
        SetVisible(obsAvailable && sendObsScene && _obsSceneRows.Count == 0, RuleObsEmptyHintText);
        SetVisible(obsAvailable && sendObsMedia, ObsMediaDetailsPanel);
        SetVisible(obsAvailable && sendObsMedia && obsMediaKind == ObsMediaKind.Image, ObsMediaDurationPanel);
        SetVisible(obsAvailable && sendObsMedia && obsMediaSourceMode == MediaSourceMode.Single && obsMediaHasAssets, RuleObsMediaAssetPanel);
        SetVisible(obsAvailable && sendObsMedia && obsMediaSourceMode == MediaSourceMode.Group && obsMediaHasGroups, RuleObsMediaGroupPanel);
        SetVisible(obsAvailable && sendObsMedia
            && ((obsMediaSourceMode == MediaSourceMode.Single && !obsMediaHasAssets)
                || (obsMediaSourceMode == MediaSourceMode.Group && !obsMediaHasGroups)), RuleObsMediaEmptyHintText);

        SetVisible(useLights, LightConfigurationPanel, LightOptionsSeparator, TargetPinsLabel, TargetPinsChoiceBox, PatternGrid, RuleLedPreviewPanel);
        var usesAnyLightColor = useLights
            && (LightPatternCapabilities.UsesPrimaryColor(pattern)
                || LightPatternCapabilities.UsesSecondaryColor(pattern)
                || LightPatternCapabilities.UsesTertiaryColor(pattern));
        SetVisible(usesAnyLightColor, ColorOptionsGrid);
        SetVisible(useLights && LightPatternCapabilities.UsesPrimaryColor(pattern), PrimaryColorPanel);
        SetVisible(useLights && LightPatternCapabilities.UsesSecondaryColor(pattern), SecondaryColorLabel, SecondaryColorPanel);
        SetVisible(useLights && LightPatternCapabilities.UsesTertiaryColor(pattern), TertiaryColorLabel, TertiaryColorPanel);
        SetVisible(useLights && LightPatternCapabilities.UsesBrightness(pattern), BrightnessGrid);
        SetVisible(useLights && !playAudio, DurationGrid);
        SetVisible(useLights && LightPatternCapabilities.UsesCycle(pattern), CycleGrid);
        SetVisible(useLights && LightPatternCapabilities.UsesStep(pattern), StepGrid);
        UpdateRuleAudioModeSelection();
        UpdateRuleObsMediaModeSelection();
        RefreshRuleObsMediaChoices();
        UpdateRuleLedPreviewFrame();
        UpdateRuleLedPreviewTimerState();
    }

    private void RefreshRuleObsMediaChoices()
    {
        if (_initializingComponent)
        {
            return;
        }

        var kind = RuleObsMediaKindBox.SelectedValue is ObsMediaKind selectedKind
            ? selectedKind
            : ObsMediaKind.Image;

        if (kind == ObsMediaKind.Image)
        {
            RuleObsMediaAssetBox.ItemsSource = _config.ImageLibrary;
            RuleObsMediaGroupBox.ItemsSource = _config.ImageGroups;
        }
        else
        {
            RuleObsMediaAssetBox.ItemsSource = _config.VideoLibrary;
            RuleObsMediaGroupBox.ItemsSource = _config.VideoGroups;
        }

        RuleObsMediaAssetBox.DisplayMemberPath = nameof(MediaAssetConfig.DisplayName);
        RuleObsMediaAssetBox.SelectedValuePath = nameof(MediaAssetConfig.Id);
        RuleObsMediaGroupBox.DisplayMemberPath = nameof(MediaGroupConfig.Name);
        RuleObsMediaGroupBox.SelectedValuePath = nameof(MediaGroupConfig.Id);
    }

    private void UpdateBackgroundOptionVisibility()
    {
        var arduinoAvailable = _config.ArduinoEnabled;
        var enabled = arduinoAvailable && BackgroundEnabledCheck.IsChecked == true;
        var alexaEnabled = BackgroundAlexaEnabledCheck.IsChecked == true;
        var alexaTurnOffAfterEvent = BackgroundAlexaTurnOffAfterEventCheck.IsChecked == true;
        var alexaAvailable = _config.Alexa.IsConfigured;
        var pattern = BackgroundPatternBox.SelectedValue is LightPattern selectedPattern
            ? selectedPattern
            : LightPattern.Solid;

        SetVisible(alexaAvailable, BackgroundAlexaEnabledCheck, BackgroundAlexaTurnOffAfterEventCheck, StopAlexaBackgroundButton);
        SetVisible(!alexaAvailable, AlexaBackgroundUnavailableText);
        SetVisible(alexaAvailable && (alexaEnabled || alexaTurnOffAfterEvent), BackgroundAlexaEventsGrid, ApplyAlexaBackgroundButton);
        SetVisible(arduinoAvailable, BackgroundEnabledCheck);
        SetVisible(enabled, BackgroundPatternGrid, BackgroundLedPreviewPanel, ApplyArduinoBackgroundButton);
        var usesAnyBackgroundColor = enabled
            && (LightPatternCapabilities.UsesPrimaryColor(pattern)
                || LightPatternCapabilities.UsesSecondaryColor(pattern)
                || LightPatternCapabilities.UsesTertiaryColor(pattern));
        SetVisible(usesAnyBackgroundColor, BackgroundColorOptionsGrid);
        SetVisible(enabled && LightPatternCapabilities.UsesBrightness(pattern), BackgroundBrightnessPanel);
        SetVisible(enabled && LightPatternCapabilities.UsesPrimaryColor(pattern), BackgroundPrimaryColorLabel, BackgroundPrimaryColorPanel);
        SetVisible(enabled && LightPatternCapabilities.UsesSecondaryColor(pattern), BackgroundSecondaryColorLabel, BackgroundSecondaryColorPanel);
        SetVisible(enabled && LightPatternCapabilities.UsesTertiaryColor(pattern), BackgroundTertiaryColorLabel, BackgroundTertiaryColorPanel);
        SetVisible(enabled && LightPatternCapabilities.UsesCycle(pattern), BackgroundCycleGrid);
        SetVisible(enabled && LightPatternCapabilities.UsesStep(pattern), BackgroundStepGrid);
    }

    private void ApplyBackgroundOutputMode()
    {
        if (_initializingComponent)
        {
            return;
        }

        UpdateBackgroundOptionVisibility();
        UpdateBackgroundLedPreviewTimerState();
    }

    private static void SetVisible(bool isVisible, params UIElement[] elements)
    {
        var visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
        foreach (var element in elements)
        {
            element.Visibility = visibility;
        }
    }

    private void UpdateRuleLedPreviewFrame()
    {
        if (_initializingComponent)
        {
            return;
        }

        if (!Dispatcher.CheckAccess())
        {
            _ = Dispatcher.InvokeAsync(UpdateRuleLedPreviewFrame);
            return;
        }

        if (!ShouldRunRuleLedPreview())
        {
            UpdateRuleLedPreviewTimerState();
            return;
        }

        ResizeLedPreviewDots(_ruleLedPreviewDots, RuleLedPreviewPanel.ActualWidth);
        var pattern = PatternBox.SelectedValue is LightPattern selectedPattern
            ? selectedPattern
            : LightPattern.Pulse;
        var brightness = Math.Clamp(BrightnessSlider.Value / 255d, 0d, 1d);
        var primary = LedPreviewService.ParseColor(PrimaryColorBox.Text, "#14B8A6");
        var secondary = LedPreviewService.ParseColor(SecondaryColorBox.Text, "#B56CFF");
        var tertiary = LedPreviewService.ParseColor(TertiaryColorBox.Text, "#FFFFFF");
        var count = _ruleLedPreviewDots.Count;
        _ruleLedPreviewStep++;
        var frame = LedPreviewService.BuildFrame(pattern, _ruleLedPreviewStep, count, brightness, primary, secondary, tertiary, _previewRandom);

        for (var i = 0; i < count; i++)
        {
            _ruleLedPreviewDots[i] = PreviewDot(frame[i], brightness);
        }
    }

    private void SetRuleLedPreviewAll(string color)
    {
        ResizeLedPreviewDots(_ruleLedPreviewDots, RuleLedPreviewPanel.ActualWidth);
        var previewColor = LedPreviewService.ParseColor(color, "#334155");
        for (var i = 0; i < _ruleLedPreviewDots.Count; i++)
        {
            _ruleLedPreviewDots[i] = PreviewDot(previewColor, 0.08);
        }
    }

    private void UpdateRuleLedPreviewTimerState()
    {
        if (_initializingComponent || !Dispatcher.CheckAccess())
        {
            return;
        }

        var shouldRun = ShouldRunRuleLedPreview();
        if (shouldRun)
        {
            if (!_ruleLedPreviewTimer.IsEnabled)
            {
                _ruleLedPreviewTimer.Start();
            }

            return;
        }

        if (_ruleLedPreviewTimer.IsEnabled)
        {
            _ruleLedPreviewTimer.Stop();
        }

        if (UseLightsCheck.IsChecked != true || !_config.ArduinoEnabled)
        {
            SetRuleLedPreviewAll("#334155");
        }
    }

    private bool ShouldRunRuleLedPreview()
    {
        return UseLightsCheck.IsChecked == true
            && _config.ArduinoEnabled
            && MainTabs.SelectedIndex == 2
            && LightConfigurationPanel.IsExpanded
            && RuleLedPreviewPanel.IsVisible;
    }

    private void UpdateBackgroundLedPreviewFrame()
    {
        if (_initializingComponent)
        {
            return;
        }

        if (!Dispatcher.CheckAccess())
        {
            _ = Dispatcher.InvokeAsync(UpdateBackgroundLedPreviewFrame);
            return;
        }

        if (!ShouldRunBackgroundLedPreview())
        {
            UpdateBackgroundLedPreviewTimerState();
            return;
        }

        ResizeLedPreviewDots(_backgroundLedPreviewDots, BackgroundLedPreviewPanel.ActualWidth);
        var pattern = BackgroundPatternBox.SelectedValue is LightPattern selectedPattern
            ? selectedPattern
            : LightPattern.Solid;
        var brightness = Math.Clamp(BackgroundBrightnessSlider.Value / 255d, 0d, 1d);
        var primary = LedPreviewService.ParseColor(BackgroundPrimaryColorBox.Text, "#14B8A6");
        var secondary = LedPreviewService.ParseColor(BackgroundSecondaryColorBox.Text, "#B56CFF");
        var tertiary = LedPreviewService.ParseColor(BackgroundTertiaryColorBox.Text, "#FFFFFF");
        var count = _backgroundLedPreviewDots.Count;
        _backgroundLedPreviewStep++;
        var frame = LedPreviewService.BuildFrame(pattern, _backgroundLedPreviewStep, count, brightness, primary, secondary, tertiary, _previewRandom);

        for (var i = 0; i < count; i++)
        {
            _backgroundLedPreviewDots[i] = PreviewDot(frame[i], brightness);
        }
    }

    private void SetBackgroundLedPreviewAll(string color)
    {
        ResizeLedPreviewDots(_backgroundLedPreviewDots, BackgroundLedPreviewPanel.ActualWidth);
        var previewColor = LedPreviewService.ParseColor(color, "#334155");
        for (var i = 0; i < _backgroundLedPreviewDots.Count; i++)
        {
            _backgroundLedPreviewDots[i] = PreviewDot(previewColor, 0.08);
        }
    }

    private void UpdateBackgroundLedPreviewTimerState()
    {
        if (_initializingComponent || !Dispatcher.CheckAccess())
        {
            return;
        }

        var shouldRun = ShouldRunBackgroundLedPreview();
        if (shouldRun)
        {
            if (!_backgroundLedPreviewTimer.IsEnabled)
            {
                _backgroundLedPreviewTimer.Start();
            }

            return;
        }

        if (_backgroundLedPreviewTimer.IsEnabled)
        {
            _backgroundLedPreviewTimer.Stop();
        }

        if (BackgroundEnabledCheck.IsChecked != true || !_config.ArduinoEnabled)
        {
            SetBackgroundLedPreviewAll("#334155");
        }
    }

    private bool ShouldRunBackgroundLedPreview()
    {
        return BackgroundEnabledCheck.IsChecked == true
            && _config.ArduinoEnabled
            && MainTabs.SelectedIndex == 3
            && BackgroundLedPreviewPanel.IsVisible;
    }

    private static RuleLedPreviewDot PreviewDot(System.Windows.Media.Color color, double brightness)
    {
        var glowOpacity = Math.Clamp(0.12 + (brightness * 0.72), 0.12, 0.9);
        var glowRadius = 7d + (brightness * 22d);
        return new RuleLedPreviewDot(
            FrozenBrushFrom($"#{color.R:X2}{color.G:X2}{color.B:X2}"),
            color,
            glowOpacity,
            glowRadius);
    }

    private static void ResizeLedPreviewDots(ObservableCollection<RuleLedPreviewDot> dots, double availableWidth)
    {
        var targetCount = LedPreviewService.CalculateDotCount(availableWidth);
        while (dots.Count < targetCount)
        {
            dots.Add(PreviewDot(LedPreviewService.ParseColor("#334155", "#334155"), 0.08));
        }

        while (dots.Count > targetCount)
        {
            dots.RemoveAt(dots.Count - 1);
        }
    }

}
