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
        UiVisibilityService.SetVisible(visibility.ShowVirtualLightsAction, VirtualLightsActionCard);
        UiVisibilityService.SetVisible(visibility.ShowVirtualLightsDetails, VirtualLightsDetailsPanel);
        UiVisibilityService.SetVisible(_alertsViewModel.Editor.VirtualLightsToObs, VirtualObsOptionsPanel);
        UiVisibilityService.SetVisible(_alertsViewModel.Editor.VirtualLightsToScreen, VirtualScreenOptionsPanel);
        UiVisibilityService.SetVisible(visibility.ShowVirtualLightColorOptions, VirtualColorOptionsGrid);
        UiVisibilityService.SetVisible(visibility.ShowVirtualPrimaryColor, VirtualPrimaryColorPanel);
        UiVisibilityService.SetVisible(visibility.ShowVirtualSecondaryColor, VirtualSecondaryColorLabel, VirtualSecondaryColorPanel);
        UiVisibilityService.SetVisible(visibility.ShowVirtualTertiaryColor, VirtualTertiaryColorLabel, VirtualTertiaryColorPanel);
        UiVisibilityService.SetVisible(visibility.ShowVirtualLightConfiguration, VirtualLedPreviewTitle, VirtualRuleLedPreviewPanel);
        UiVisibilityService.SetVisible(
            visibility.ShowVirtualBrightness || visibility.ShowVirtualDuration || visibility.ShowVirtualCycle || visibility.ShowVirtualStep,
            VirtualTimingTitle);
        UiVisibilityService.SetVisible(visibility.ShowVirtualBrightness, VirtualBrightnessGrid);
        UiVisibilityService.SetVisible(visibility.ShowVirtualDuration, VirtualDurationGrid);
        UiVisibilityService.SetVisible(visibility.ShowVirtualCycle, VirtualCycleGrid);
        UiVisibilityService.SetVisible(visibility.ShowVirtualStep, VirtualStepGrid);
        UiVisibilityService.SetVisible(visibility.ShowObsVideoAction, VideoActionCard);
        UiVisibilityService.SetVisible(visibility.ShowObsVideoDetails, ObsVideoDetailsPanel);
        UiVisibilityService.SetVisible(visibility.ShowObsVideoAsset, RuleObsVideoAssetPanel);
        UiVisibilityService.SetVisible(visibility.ShowObsVideoGroup, RuleObsVideoGroupPanel);
        UiVisibilityService.SetVisible(visibility.ShowObsVideoEmptyHint, RuleObsVideoEmptyHintText);
        UiVisibilityService.SetVisible(visibility.ShowObsImageAction, ImageActionCard);
        UiVisibilityService.SetVisible(visibility.ShowObsImageDetails, ObsImageDetailsPanel, ObsImageDurationPanel);
        UiVisibilityService.SetVisible(visibility.ShowObsImageAsset, RuleObsImageAssetPanel);
        UiVisibilityService.SetVisible(visibility.ShowObsImageGroup, RuleObsImageGroupPanel);
        UiVisibilityService.SetVisible(visibility.ShowObsImageEmptyHint, RuleObsImageEmptyHintText);
        UiVisibilityService.SetVisible(visibility.ShowObsAction, ObsActionCard);
        UiVisibilityService.SetVisible(visibility.ShowObsDetails, ObsDetailsPanel);
        UiVisibilityService.SetVisible(visibility.ShowObsSceneDetails, ObsSceneDetailsPanel);
        UiVisibilityService.SetVisible(visibility.ShowObsSceneTiming, ObsSceneTimingGrid);
        UiVisibilityService.SetVisible(visibility.ShowObsReturnDelay, ObsReturnDelayPanel);
        UiVisibilityService.SetVisible(visibility.ShowObsEmptyHint, RuleObsEmptyHintText);
        UiVisibilityService.SetVisible(visibility.ShowAlexaAction, AlexaActionCard);
        UiVisibilityService.SetVisible(visibility.ShowAlexaDetails, AlexaDetailsPanel, AlexaRuleHintText);
        UiVisibilityService.SetVisible(visibility.ShowLightConfiguration, LightConfigurationPanel, LightOptionsSeparator, PatternGrid, RuleLedPreviewPanel);
        UiVisibilityService.SetVisible(visibility.ShowTargetPins, TargetPinsLabel, TargetPinsChoiceBox);
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
        var virtualLightsEnabled = editor.UseVirtualLights;
        var audioEnabled = editor.PlayAudio;
        var chatEnabled = editor.SendChatMessage;
        var alexaEnabled = _config.Alexa.IsConfigured && editor.SendAlexaEvent;
        var obsSceneEnabled = _config.Obs.IsConfigured && editor.SendObsScene;
        var obsImageEnabled = _config.Obs.IsConfigured && editor.SendObsImage;
        var obsVideoEnabled = _config.Obs.IsConfigured && editor.SendObsVideo;

        SetRuleOptionAvailability(lightsEnabled, LightConfigurationPanel);
        SetRuleOptionAvailability(virtualLightsEnabled, VirtualLightsDetailsPanel);
        SetRuleOptionAvailability(audioEnabled, AudioDetailsPanel);
        SetRuleOptionAvailability(chatEnabled, ChatDetailsPanel);
        SetRuleOptionAvailability(alexaEnabled, AlexaDetailsPanel);
        SetRuleOptionAvailability(obsSceneEnabled, ObsSceneDetailsPanel);
        SetRuleOptionAvailability(obsImageEnabled, ObsImageDetailsPanel);
        SetRuleOptionAvailability(obsVideoEnabled, ObsVideoDetailsPanel);
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
