using NeoTwitch.Models;
using NeoTwitch.Services.Lights;
using NeoTwitch.Services.Ui;
using NeoTwitch.Shared;
using static NeoTwitch.Services.InputValueParser;

namespace NeoTwitch;

public partial class MainWindow
{
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

        var hasAudios = _config.AudioLibrary.Count > 0;
        var hasGroups = _config.AudioGroups.Count > 0;
        var visibility = OptionVisibilityService.ResolveRule(new RuleOptionVisibilityInput(
            kind,
            arduinoAvailable,
            useLights,
            playAudio,
            _ruleAudioMode,
            hasAudios,
            hasGroups,
            sendChat,
            alexaAvailable,
            sendAlexa,
            obsAvailable,
            sendObsScene,
            selectedObsSceneName,
            returnObsScene,
            _obsSceneRows.Count > 0,
            sendObsMedia,
            obsMediaKind,
            obsMediaSourceMode,
            obsMediaHasAssets,
            obsMediaHasGroups,
            pattern));

        SetVisible(visibility.ShowRewardTitle, RewardTitleLabel, RewardTitleBox);
        SetVisible(visibility.ShowChatCommand, ChatCommandLabel, ChatCommandBox);
        SetVisible(visibility.ShowMinimumBits, MinimumBitsLabel, MinimumBitsBox);
        SetVisible(visibility.ShowAudioDetails, AudioDetailsPanel, AudioLabel, AudioPanel);
        SetVisible(visibility.ShowAudioSingle, RuleAudioSinglePanel);
        SetVisible(visibility.ShowAudioGroup, RuleAudioGroupPanel);
        SetVisible(visibility.ShowAudioEmptyHint, RuleAudioEmptyHintText);
        SetVisible(visibility.ShowChatDetails, ChatDetailsPanel, ChatMessageLabel, ChatMessageBox);
        SetVisible(visibility.ShowLightsAction, UseLightsActionCard);
        SetVisible(visibility.ShowAlexaAction, AlexaActionCard);
        SetVisible(visibility.ShowAlexaDetails, AlexaDetailsPanel, AlexaRuleHintText);
        SetVisible(visibility.ShowObsAction, ObsActionCard);
        SetVisible(visibility.ShowObsDetails, ObsDetailsPanel);
        SetVisible(visibility.ShowObsSceneDetails, ObsSceneDetailsPanel);
        SetVisible(visibility.ShowObsSceneTiming, ObsSceneTimingGrid);
        SetVisible(visibility.ShowObsReturnDelay, ObsReturnDelayPanel);
        SetVisible(visibility.ShowObsEmptyHint, RuleObsEmptyHintText);
        SetVisible(visibility.ShowObsMediaDetails, ObsMediaDetailsPanel);
        SetVisible(visibility.ShowObsMediaDuration, ObsMediaDurationPanel);
        SetVisible(visibility.ShowObsMediaAsset, RuleObsMediaAssetPanel);
        SetVisible(visibility.ShowObsMediaGroup, RuleObsMediaGroupPanel);
        SetVisible(visibility.ShowObsMediaEmptyHint, RuleObsMediaEmptyHintText);
        SetVisible(visibility.ShowLightConfiguration, LightConfigurationPanel, LightOptionsSeparator, TargetPinsLabel, TargetPinsChoiceBox, PatternGrid, RuleLedPreviewPanel);
        SetVisible(visibility.ShowLightColorOptions, ColorOptionsGrid);
        SetVisible(visibility.ShowPrimaryColor, PrimaryColorPanel);
        SetVisible(visibility.ShowSecondaryColor, SecondaryColorLabel, SecondaryColorPanel);
        SetVisible(visibility.ShowTertiaryColor, TertiaryColorLabel, TertiaryColorPanel);
        SetVisible(visibility.ShowBrightness, BrightnessGrid);
        SetVisible(visibility.ShowDuration, DurationGrid);
        SetVisible(visibility.ShowCycle, CycleGrid);
        SetVisible(visibility.ShowStep, StepGrid);
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
}
