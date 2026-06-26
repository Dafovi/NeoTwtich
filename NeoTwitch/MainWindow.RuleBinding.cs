using NeoTwitch.Models;
using NeoTwitch.Services.Alerts;
using NeoTwitch.Services.Lights;
using NeoTwitch.Services.Ui;
using NeoTwitch.Shared;

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

        var mediaKind = RuleObsMediaKindBox.SelectedValue is ObsMediaKind selectedMediaKind
            ? selectedMediaKind
            : ObsMediaKind.Image;
        var mediaSourceMode = RuleObsMediaSourceModeBox.SelectedValue is MediaSourceMode selectedMediaSourceMode
            ? selectedMediaSourceMode
            : MediaSourceMode.Single;
        RefreshRuleObsMediaChoices();

        var pattern = PatternBox.SelectedValue is LightPattern selectedPattern
            ? selectedPattern
            : LightPattern.Pulse;

        RuleEditorFormService.Apply(
            rule,
            new RuleEditorFormValues(
                RuleEnabledCheck.IsChecked == true,
                RuleNameBox.Text,
                kind,
                RewardTitleBox.Text,
                ChatCommandBox.Text,
                MinimumBitsBox.Text,
                ChatMessageCheck.IsChecked == true,
                ChatMessageBox.Text,
                AlexaEventCheck.IsChecked == true,
                ObsSceneCheck.IsChecked == true,
                RuleObsSceneBox.SelectedValue as string ?? RuleObsSceneBox.Text,
                ObsSceneDelayBox.Text,
                ObsReturnCheck.IsChecked == true,
                ObsReturnDelayBox.Text,
                ObsMediaCheck.IsChecked == true,
                mediaKind,
                mediaSourceMode,
                RuleObsMediaAssetBox.SelectedValue as string ?? "",
                RuleObsMediaGroupBox.SelectedValue as string ?? "",
                ObsMediaDurationBox.Text,
                UseLightsCheck.IsChecked == true,
                PlayAudioCheck.IsChecked == true,
                _ruleAudioMode,
                RuleAudioAssetBox.SelectedValue as string ?? "",
                RuleAudioGroupBox.SelectedValue as string ?? "",
                pattern,
                TargetPinsBox.Text,
                PrimaryColorBox.Text,
                SecondaryColorBox.Text,
                TertiaryColorBox.Text,
                BrightnessSlider.Value,
                DurationSlider.Value,
                CycleSlider.Value,
                StepSlider.Value),
            _config.AudioLibrary);

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
        var obsMediaChoices = RuleObsMediaChoiceService.Resolve(
            obsMediaKind,
            _config.ImageLibrary,
            _config.VideoLibrary,
            _config.ImageGroups,
            _config.VideoGroups);
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
            obsMediaChoices.HasAssets,
            obsMediaChoices.HasGroups,
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

        var choices = RuleObsMediaChoiceService.Resolve(
            kind,
            _config.ImageLibrary,
            _config.VideoLibrary,
            _config.ImageGroups,
            _config.VideoGroups);

        RuleObsMediaAssetBox.ItemsSource = choices.Assets;
        RuleObsMediaGroupBox.ItemsSource = choices.Groups;

        RuleObsMediaAssetBox.DisplayMemberPath = nameof(MediaAssetConfig.DisplayName);
        RuleObsMediaAssetBox.SelectedValuePath = nameof(MediaAssetConfig.Id);
        RuleObsMediaGroupBox.DisplayMemberPath = nameof(MediaGroupConfig.Name);
        RuleObsMediaGroupBox.SelectedValuePath = nameof(MediaGroupConfig.Id);
    }
}
