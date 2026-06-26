using NeoTwitch.Models;
using NeoTwitch.Services.Alerts;
using NeoTwitch.Services.Ui;
using NeoTwitch.ViewModels.Library;

namespace NeoTwitch;

public partial class MainWindow
{
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

        UiVisibilityService.SetVisible(visibility.ShowRewardTitle, RewardTitleLabel, RewardTitleBox);
        UiVisibilityService.SetVisible(visibility.ShowChatCommand, ChatCommandLabel, ChatCommandBox);
        UiVisibilityService.SetVisible(visibility.ShowMinimumBits, MinimumBitsLabel, MinimumBitsBox);
        UiVisibilityService.SetVisible(visibility.ShowAudioDetails, AudioDetailsPanel, AudioLabel, AudioPanel);
        UiVisibilityService.SetVisible(visibility.ShowAudioSingle, RuleAudioSinglePanel);
        UiVisibilityService.SetVisible(visibility.ShowAudioGroup, RuleAudioGroupPanel);
        UiVisibilityService.SetVisible(visibility.ShowAudioEmptyHint, RuleAudioEmptyHintText);
        UiVisibilityService.SetVisible(visibility.ShowChatDetails, ChatDetailsPanel, ChatMessageLabel, ChatMessageBox);
        UiVisibilityService.SetVisible(visibility.ShowLightsAction, UseLightsActionCard);
        UiVisibilityService.SetVisible(visibility.ShowAlexaAction, AlexaActionCard);
        UiVisibilityService.SetVisible(visibility.ShowAlexaDetails, AlexaDetailsPanel, AlexaRuleHintText);
        UiVisibilityService.SetVisible(visibility.ShowObsAction, ObsActionCard);
        UiVisibilityService.SetVisible(visibility.ShowObsDetails, ObsDetailsPanel);
        UiVisibilityService.SetVisible(visibility.ShowObsSceneDetails, ObsSceneDetailsPanel);
        UiVisibilityService.SetVisible(visibility.ShowObsSceneTiming, ObsSceneTimingGrid);
        UiVisibilityService.SetVisible(visibility.ShowObsReturnDelay, ObsReturnDelayPanel);
        UiVisibilityService.SetVisible(visibility.ShowObsEmptyHint, RuleObsEmptyHintText);
        UiVisibilityService.SetVisible(visibility.ShowObsMediaDetails, ObsMediaDetailsPanel);
        UiVisibilityService.SetVisible(visibility.ShowObsMediaDuration, ObsMediaDurationPanel);
        UiVisibilityService.SetVisible(visibility.ShowObsMediaAsset, RuleObsMediaAssetPanel);
        UiVisibilityService.SetVisible(visibility.ShowObsMediaGroup, RuleObsMediaGroupPanel);
        UiVisibilityService.SetVisible(visibility.ShowObsMediaEmptyHint, RuleObsMediaEmptyHintText);
        UiVisibilityService.SetVisible(visibility.ShowLightConfiguration, LightConfigurationPanel, LightOptionsSeparator, TargetPinsLabel, TargetPinsChoiceBox, PatternGrid, RuleLedPreviewPanel);
        UiVisibilityService.SetVisible(visibility.ShowLightColorOptions, ColorOptionsGrid);
        UiVisibilityService.SetVisible(visibility.ShowPrimaryColor, PrimaryColorPanel);
        UiVisibilityService.SetVisible(visibility.ShowSecondaryColor, SecondaryColorLabel, SecondaryColorPanel);
        UiVisibilityService.SetVisible(visibility.ShowTertiaryColor, TertiaryColorLabel, TertiaryColorPanel);
        UiVisibilityService.SetVisible(visibility.ShowBrightness, BrightnessGrid);
        UiVisibilityService.SetVisible(visibility.ShowDuration, DurationGrid);
        UiVisibilityService.SetVisible(visibility.ShowCycle, CycleGrid);
        UiVisibilityService.SetVisible(visibility.ShowStep, StepGrid);
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
