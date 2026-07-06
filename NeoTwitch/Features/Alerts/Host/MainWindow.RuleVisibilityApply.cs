using System.Windows;
using NeoTwitch.Services.Ui;

namespace NeoTwitch;

public partial class MainWindow
{
    private void ApplyRuleOptionVisibility(RuleOptionVisibility visibility)
    {
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

        ApplyRuleOptionEnabledState();
    }

    private void ApplyRuleOptionEnabledState()
    {
        var editor = _alertsViewModel.Editor;
        var lightsEnabled = _config.ArduinoEnabled && editor.UseLights;
        var audioEnabled = editor.PlayAudio;
        var chatEnabled = editor.SendChatMessage;
        var alexaEnabled = _config.Alexa.IsConfigured && editor.SendAlexaEvent;
        var obsSceneEnabled = _config.Obs.IsConfigured && editor.SendObsScene;
        var obsMediaEnabled = _config.Obs.IsConfigured && editor.SendObsMedia;

        SetRuleOptionAvailability(lightsEnabled, LightConfigurationPanel);
        SetRuleOptionAvailability(audioEnabled, AudioDetailsPanel);
        SetRuleOptionAvailability(chatEnabled, ChatDetailsPanel);
        SetRuleOptionAvailability(alexaEnabled, AlexaDetailsPanel);
        SetRuleOptionAvailability(obsSceneEnabled, ObsSceneDetailsPanel);
        SetRuleOptionAvailability(obsMediaEnabled, ObsMediaDetailsPanel);
    }

    private static void SetRuleOptionAvailability(bool enabled, params FrameworkElement[] elements)
    {
        foreach (var element in elements)
        {
            element.IsEnabled = enabled;
            element.Opacity = enabled ? 1d : 0.42d;
        }
    }
}
