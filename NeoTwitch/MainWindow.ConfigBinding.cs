using System.IO;
using System.Windows;
using NeoTwitch.Models;
using NeoTwitch.Services;
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
            RefreshRulesView();
            StripsList.ItemsSource = _config.LedStrips;
            SettingsPathText.Text = _settingsStore.SettingsPath;
            BackupPathText.Text = $"Backups automaticos: {_settingsStore.BackupDirectory}";
            SettingsVersionText.Text = $"V{VersionCheckService.CurrentVersionText}";
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
                return;
            }

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
            UpdateRuleLedPreviewFrame();
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
        _config.ThemeMode = NormalizeThemeMode(ThemeModeBox.SelectedValue as string ?? _config.ThemeMode);
        _config.DarkMode = ResolveDarkMode(_config.ThemeMode);
        _config.CloseToTray = CloseToTrayCheck.IsChecked == true;
        _config.AlertVolumePercent = (int)Math.Round(AlertVolumeSlider.Value);
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
    }

    private void ApplyStartWithWindowsRegistration()
    {
        if (_lastAppliedStartWithWindows == _config.StartWithWindows)
        {
            return;
        }

        try
        {
            using var runKey = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(WindowsRunKeyPath, writable: true);
            if (runKey is null)
            {
                throw new InvalidOperationException("No pude abrir la clave de inicio de Windows.");
            }

            if (_config.StartWithWindows)
            {
                var executablePath = Environment.ProcessPath;
                if (string.IsNullOrWhiteSpace(executablePath) || !File.Exists(executablePath))
                {
                    throw new InvalidOperationException("No pude detectar la ruta del ejecutable actual.");
                }

                runKey.SetValue(WindowsStartupValueName, $"\"{executablePath}\"");
            }
            else
            {
                runKey.DeleteValue(WindowsStartupValueName, throwOnMissingValue: false);
            }

            _lastAppliedStartWithWindows = _config.StartWithWindows;
        }
        catch (Exception ex)
        {
            AddLog($"Inicio con Windows: {ex.Message}", ActivityLogKind.Important);
        }
    }

    private void SaveCurrentRuleFromFields()
    {
        if (_loadingRule || RulesList.SelectedItem is not EventRule rule)
        {
            return;
        }

        rule.IsEnabled = RuleEnabledCheck.IsChecked == true;
        rule.Name = RuleNameBox.Text.Trim();
        rule.EventKind = EventKindBox.SelectedValue is TwitchEventKind kind ? kind : TwitchEventKind.Follow;
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
        UpdateRuleLedPreviewFrame();
        UpdateRuleOptionVisibility();
        UpdateRuleLedPreviewTimerState();
        RefreshRulesView();
        RefreshAudioLibraryView();
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
        SetVisible(obsAvailable && sendObsScene && _obsSceneRows.Count == 0, RuleObsEmptyHintText);
        SetVisible(obsAvailable && sendObsMedia, ObsMediaDetailsPanel);
        SetVisible(obsAvailable && sendObsMedia && obsMediaSourceMode == MediaSourceMode.Single && obsMediaHasAssets, RuleObsMediaAssetPanel);
        SetVisible(obsAvailable && sendObsMedia && obsMediaSourceMode == MediaSourceMode.Group && obsMediaHasGroups, RuleObsMediaGroupPanel);
        SetVisible(obsAvailable && sendObsMedia
            && ((obsMediaSourceMode == MediaSourceMode.Single && !obsMediaHasAssets)
                || (obsMediaSourceMode == MediaSourceMode.Group && !obsMediaHasGroups)), RuleObsMediaEmptyHintText);

        SetVisible(useLights, LightConfigurationPanel, LightOptionsSeparator, TargetPinsLabel, TargetPinsBox, PatternGrid, RuleLedPreviewPanel);
        SetVisible(useLights && UsesPrimaryColor(pattern), PrimaryColorPanel);
        SetVisible(useLights && UsesSecondaryColor(pattern), SecondaryColorLabel, SecondaryColorPanel);
        SetVisible(useLights && UsesTertiaryColor(pattern), TertiaryColorLabel, TertiaryColorPanel);
        SetVisible(useLights && UsesBrightness(pattern), BrightnessGrid, BrightnessSlider);
        SetVisible(useLights && !playAudio, DurationGrid, DurationSlider);
        SetVisible(useLights && UsesCycle(pattern), CycleGrid, CycleSlider);
        SetVisible(useLights && UsesStep(pattern), StepGrid, StepSlider);
        UpdateRuleAudioModeSelection();
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
        SetVisible(enabled && UsesBrightness(pattern), BackgroundBrightnessPanel);
        SetVisible(enabled && UsesPrimaryColor(pattern), BackgroundPrimaryColorLabel, BackgroundPrimaryColorPanel);
        SetVisible(enabled && UsesSecondaryColor(pattern), BackgroundSecondaryColorLabel, BackgroundSecondaryColorPanel);
        SetVisible(enabled && UsesTertiaryColor(pattern), BackgroundTertiaryColorLabel, BackgroundTertiaryColorPanel);
        SetVisible(enabled && UsesCycle(pattern), BackgroundCycleGrid, BackgroundCycleSlider);
        SetVisible(enabled && UsesStep(pattern), BackgroundStepGrid, BackgroundStepSlider);
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
        if (_initializingComponent || _ruleLedPreviewDots.Count == 0)
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

        var pattern = PatternBox.SelectedValue is LightPattern selectedPattern
            ? selectedPattern
            : LightPattern.Pulse;
        var brightness = Math.Clamp(BrightnessSlider.Value / 255d, 0d, 1d);
        var colorScale = Math.Clamp(brightness, 0.08, 1d);
        var primary = ParsePreviewColor(PrimaryColorBox.Text, "#14B8A6");
        var secondary = ParsePreviewColor(SecondaryColorBox.Text, "#B56CFF");
        var tertiary = ParsePreviewColor(TertiaryColorBox.Text, "#FFFFFF");
        var count = _ruleLedPreviewDots.Count;
        _ruleLedPreviewStep++;

        for (var i = 0; i < count; i++)
        {
            var phase = (i + _ruleLedPreviewStep) / (double)count;
            var color = pattern switch
            {
                LightPattern.Solid => primary,
                LightPattern.Rainbow => RainbowPreviewColor(phase),
                LightPattern.Pulse => BlendPreviewColor(primary, secondary, (Math.Sin((_ruleLedPreviewStep * 0.18) + (i * 0.22)) + 1d) / 2d),
                LightPattern.Chase => ((i + _ruleLedPreviewStep) % 6) < 2
                    ? primary
                    : ScalePreviewColor(secondary, 0.22),
                LightPattern.Theater => ((i + _ruleLedPreviewStep) % 3) == 0
                    ? primary
                    : (((i + _ruleLedPreviewStep) % 3) == 1 ? secondary : ScalePreviewColor(tertiary, 0.18)),
                LightPattern.Sparkle => _previewRandom.NextDouble() > 0.72
                    ? RandomPreviewColor(primary, secondary, tertiary)
                    : ScalePreviewColor(primary, 0.16),
                LightPattern.Rave => RandomPreviewColor(primary, secondary, tertiary),
                _ => primary
            };

            _ruleLedPreviewDots[i] = PreviewDot(ScalePreviewColor(color, colorScale), brightness);
        }
    }

    private void SetRuleLedPreviewAll(string color)
    {
        var previewColor = ParsePreviewColor(color, "#334155");
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
        if (_initializingComponent || _backgroundLedPreviewDots.Count == 0)
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

        var pattern = BackgroundPatternBox.SelectedValue is LightPattern selectedPattern
            ? selectedPattern
            : LightPattern.Solid;
        var brightness = Math.Clamp(BackgroundBrightnessSlider.Value / 255d, 0d, 1d);
        var colorScale = Math.Clamp(brightness, 0.08, 1d);
        var primary = ParsePreviewColor(BackgroundPrimaryColorBox.Text, "#14B8A6");
        var secondary = ParsePreviewColor(BackgroundSecondaryColorBox.Text, "#B56CFF");
        var tertiary = ParsePreviewColor(BackgroundTertiaryColorBox.Text, "#FFFFFF");
        var count = _backgroundLedPreviewDots.Count;
        _backgroundLedPreviewStep++;

        for (var i = 0; i < count; i++)
        {
            var phase = (i + _backgroundLedPreviewStep) / (double)count;
            var color = pattern switch
            {
                LightPattern.Solid => primary,
                LightPattern.Rainbow => RainbowPreviewColor(phase),
                LightPattern.Pulse => BlendPreviewColor(primary, secondary, (Math.Sin((_backgroundLedPreviewStep * 0.18) + (i * 0.22)) + 1d) / 2d),
                LightPattern.Chase => ((i + _backgroundLedPreviewStep) % 6) < 2
                    ? primary
                    : ScalePreviewColor(secondary, 0.22),
                LightPattern.Theater => ((i + _backgroundLedPreviewStep) % 3) == 0
                    ? primary
                    : (((i + _backgroundLedPreviewStep) % 3) == 1 ? secondary : ScalePreviewColor(tertiary, 0.18)),
                LightPattern.Sparkle => _previewRandom.NextDouble() > 0.72
                    ? RandomPreviewColor(primary, secondary, tertiary)
                    : ScalePreviewColor(primary, 0.16),
                LightPattern.Rave => RandomPreviewColor(primary, secondary, tertiary),
                _ => primary
            };

            _backgroundLedPreviewDots[i] = PreviewDot(ScalePreviewColor(color, colorScale), brightness);
        }
    }

    private void SetBackgroundLedPreviewAll(string color)
    {
        var previewColor = ParsePreviewColor(color, "#334155");
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

    private System.Windows.Media.Color RandomPreviewColor(
        System.Windows.Media.Color primary,
        System.Windows.Media.Color secondary,
        System.Windows.Media.Color tertiary)
    {
        return _previewRandom.Next(3) switch
        {
            0 => primary,
            1 => secondary,
            _ => tertiary
        };
    }

    private static System.Windows.Media.Color ParsePreviewColor(string color, string fallback)
    {
        try
        {
            return (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(LightCommand.NormalizeColor(color));
        }
        catch
        {
            return (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(fallback);
        }
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

    private static System.Windows.Media.Color ScalePreviewColor(System.Windows.Media.Color color, double factor)
    {
        factor = Math.Clamp(factor, 0d, 1d);
        return System.Windows.Media.Color.FromRgb(
            (byte)Math.Round(color.R * factor),
            (byte)Math.Round(color.G * factor),
            (byte)Math.Round(color.B * factor));
    }

    private static System.Windows.Media.Color BlendPreviewColor(
        System.Windows.Media.Color start,
        System.Windows.Media.Color end,
        double amount)
    {
        amount = Math.Clamp(amount, 0d, 1d);
        return System.Windows.Media.Color.FromRgb(
            (byte)Math.Round(start.R + ((end.R - start.R) * amount)),
            (byte)Math.Round(start.G + ((end.G - start.G) * amount)),
            (byte)Math.Round(start.B + ((end.B - start.B) * amount)));
    }

    private static System.Windows.Media.Color RainbowPreviewColor(double phase)
    {
        phase -= Math.Floor(phase);
        var h = phase * 6d;
        var x = 1d - Math.Abs((h % 2d) - 1d);
        var (r, g, b) = h switch
        {
            < 1d => (1d, x, 0d),
            < 2d => (x, 1d, 0d),
            < 3d => (0d, 1d, x),
            < 4d => (0d, x, 1d),
            < 5d => (x, 0d, 1d),
            _ => (1d, 0d, x)
        };

        return System.Windows.Media.Color.FromRgb(
            (byte)Math.Round(r * 255d),
            (byte)Math.Round(g * 255d),
            (byte)Math.Round(b * 255d));
    }

    private static bool UsesPrimaryColor(LightPattern pattern)
    {
        return pattern != LightPattern.Rainbow;
    }

    private static bool UsesSecondaryColor(LightPattern pattern)
    {
        return pattern is LightPattern.Pulse
            or LightPattern.Chase
            or LightPattern.Theater
            or LightPattern.Sparkle
            or LightPattern.Rave;
    }

    private static bool UsesTertiaryColor(LightPattern pattern)
    {
        return pattern is LightPattern.Chase
            or LightPattern.Theater
            or LightPattern.Sparkle
            or LightPattern.Rave;
    }

    private static bool UsesBrightness(LightPattern pattern)
    {
        return true;
    }

    private static bool UsesCycle(LightPattern pattern)
    {
        return pattern != LightPattern.Solid;
    }

    private static bool UsesStep(LightPattern pattern)
    {
        return pattern is LightPattern.Sparkle or LightPattern.Rave;
    }
}
