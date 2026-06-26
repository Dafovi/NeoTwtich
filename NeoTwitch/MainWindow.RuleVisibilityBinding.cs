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
