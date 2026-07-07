using NeoTwitch.Models;
using NeoTwitch.Services.Lights;

namespace NeoTwitch.Services.Ui;

public static class OptionVisibilityService
{
    public static RuleOptionVisibility ResolveRule(RuleOptionVisibilityInput input)
    {
        var lightsAvailable = input.ArduinoAvailable;
        var effectAvailable = lightsAvailable && input.UseLights;
        var usesAnyLightColor = effectAvailable
            && (LightPatternCapabilities.UsesPrimaryColor(input.Pattern)
                || LightPatternCapabilities.UsesSecondaryColor(input.Pattern)
                || LightPatternCapabilities.UsesTertiaryColor(input.Pattern));

        return new RuleOptionVisibility(
            ShowRewardTitle: input.EventKind == TwitchEventKind.ChannelPointRedemption,
            ShowChatCommand: input.EventKind == TwitchEventKind.ChatCommand,
            ShowMinimumBits: input.EventKind == TwitchEventKind.Cheer,
            ShowAudioDetails: true,
            ShowAudioSingle: input.AudioSourceMode == AudioSourceMode.Single && input.HasAudioAssets,
            ShowAudioGroup: input.AudioSourceMode == AudioSourceMode.Group && input.HasAudioGroups,
            ShowAudioEmptyHint: (input.AudioSourceMode == AudioSourceMode.Single && !input.HasAudioAssets)
                || (input.AudioSourceMode == AudioSourceMode.Group && !input.HasAudioGroups),
            ShowChatDetails: true,
            ShowLightsAction: input.ArduinoAvailable,
            ShowVirtualLightsAction: true,
            ShowVirtualLightsDetails: true,
            ShowAlexaAction: input.AlexaAvailable,
            ShowAlexaDetails: input.AlexaAvailable,
            ShowObsAction: input.ObsAvailable,
            ShowObsDetails: input.ObsAvailable,
            ShowObsSceneDetails: input.ObsAvailable,
            ShowObsSceneTiming: input.ObsAvailable && !string.IsNullOrWhiteSpace(input.SelectedObsSceneName),
            ShowObsReturnDelay: input.ObsAvailable && !input.SendObsImage && !input.SendObsVideo && input.ReturnObsScene,
            ShowObsEmptyHint: input.ObsAvailable && !input.HasObsScenes,
            ShowObsImageAction: input.ObsAvailable,
            ShowObsImageDetails: input.ObsAvailable,
            ShowObsImageAsset: input.ObsAvailable && input.ObsImageSourceMode == MediaSourceMode.Single && input.HasObsImageAssets,
            ShowObsImageGroup: input.ObsAvailable && input.ObsImageSourceMode == MediaSourceMode.Group && input.HasObsImageGroups,
            ShowObsImageEmptyHint: input.ObsAvailable
                && ((input.ObsImageSourceMode == MediaSourceMode.Single && !input.HasObsImageAssets)
                    || (input.ObsImageSourceMode == MediaSourceMode.Group && !input.HasObsImageGroups)),
            ShowObsVideoAction: input.ObsAvailable,
            ShowObsVideoDetails: input.ObsAvailable,
            ShowObsVideoAsset: input.ObsAvailable && input.ObsVideoSourceMode == MediaSourceMode.Single && input.HasObsVideoAssets,
            ShowObsVideoGroup: input.ObsAvailable && input.ObsVideoSourceMode == MediaSourceMode.Group && input.HasObsVideoGroups,
            ShowObsVideoEmptyHint: input.ObsAvailable
                && ((input.ObsVideoSourceMode == MediaSourceMode.Single && !input.HasObsVideoAssets)
                    || (input.ObsVideoSourceMode == MediaSourceMode.Group && !input.HasObsVideoGroups)),
            ShowLightConfiguration: effectAvailable,
            ShowTargetPins: lightsAvailable,
            ShowLightColorOptions: usesAnyLightColor,
            ShowPrimaryColor: effectAvailable && LightPatternCapabilities.UsesPrimaryColor(input.Pattern),
            ShowSecondaryColor: effectAvailable && LightPatternCapabilities.UsesSecondaryColor(input.Pattern),
            ShowTertiaryColor: effectAvailable && LightPatternCapabilities.UsesTertiaryColor(input.Pattern),
            ShowBrightness: effectAvailable && LightPatternCapabilities.UsesBrightness(input.Pattern),
            ShowDuration: effectAvailable && !input.PlayAudio && !input.SendObsImage && !input.SendObsVideo,
            ShowCycle: effectAvailable && LightPatternCapabilities.UsesCycle(input.Pattern),
            ShowStep: effectAvailable && LightPatternCapabilities.UsesStep(input.Pattern));
    }

    public static BackgroundOptionVisibility ResolveBackground(BackgroundOptionVisibilityInput input)
    {
        var enabled = input.ArduinoAvailable && input.BackgroundEnabled;
        var usesAnyBackgroundColor = enabled
            && (LightPatternCapabilities.UsesPrimaryColor(input.Pattern)
                || LightPatternCapabilities.UsesSecondaryColor(input.Pattern)
                || LightPatternCapabilities.UsesTertiaryColor(input.Pattern));

        return new BackgroundOptionVisibility(
            ShowAlexaControls: input.AlexaAvailable,
            ShowAlexaUnavailable: !input.AlexaAvailable,
            ShowAlexaEvents: input.AlexaAvailable && (input.AlexaEnabled || input.AlexaTurnOffAfterEvent),
            ShowArduinoEnabled: input.ArduinoAvailable,
            ShowArduinoBackground: enabled,
            ShowColorOptions: usesAnyBackgroundColor,
            ShowBrightness: enabled && LightPatternCapabilities.UsesBrightness(input.Pattern),
            ShowPrimaryColor: enabled && LightPatternCapabilities.UsesPrimaryColor(input.Pattern),
            ShowSecondaryColor: enabled && LightPatternCapabilities.UsesSecondaryColor(input.Pattern),
            ShowTertiaryColor: enabled && LightPatternCapabilities.UsesTertiaryColor(input.Pattern),
            ShowCycle: enabled && LightPatternCapabilities.UsesCycle(input.Pattern),
            ShowStep: enabled && LightPatternCapabilities.UsesStep(input.Pattern));
    }
}

public sealed record RuleOptionVisibilityInput(
    TwitchEventKind EventKind,
    bool ArduinoAvailable,
    bool UseLights,
    bool UseVirtualLights,
    bool PlayAudio,
    AudioSourceMode AudioSourceMode,
    bool HasAudioAssets,
    bool HasAudioGroups,
    bool SendChatMessage,
    bool AlexaAvailable,
    bool SendAlexaEvent,
    bool ObsAvailable,
    bool SendObsScene,
    string SelectedObsSceneName,
    bool ReturnObsScene,
    bool HasObsScenes,
    bool SendObsImage,
    MediaSourceMode ObsImageSourceMode,
    bool HasObsImageAssets,
    bool HasObsImageGroups,
    bool SendObsVideo,
    MediaSourceMode ObsVideoSourceMode,
    bool HasObsVideoAssets,
    bool HasObsVideoGroups,
    LightPattern Pattern);

public sealed record RuleOptionVisibility(
    bool ShowRewardTitle,
    bool ShowChatCommand,
    bool ShowMinimumBits,
    bool ShowAudioDetails,
    bool ShowAudioSingle,
    bool ShowAudioGroup,
    bool ShowAudioEmptyHint,
    bool ShowChatDetails,
    bool ShowLightsAction,
    bool ShowVirtualLightsAction,
    bool ShowVirtualLightsDetails,
    bool ShowAlexaAction,
    bool ShowAlexaDetails,
    bool ShowObsAction,
    bool ShowObsDetails,
    bool ShowObsSceneDetails,
    bool ShowObsSceneTiming,
    bool ShowObsReturnDelay,
    bool ShowObsEmptyHint,
    bool ShowObsImageAction,
    bool ShowObsImageDetails,
    bool ShowObsImageAsset,
    bool ShowObsImageGroup,
    bool ShowObsImageEmptyHint,
    bool ShowObsVideoAction,
    bool ShowObsVideoDetails,
    bool ShowObsVideoAsset,
    bool ShowObsVideoGroup,
    bool ShowObsVideoEmptyHint,
    bool ShowLightConfiguration,
    bool ShowTargetPins,
    bool ShowLightColorOptions,
    bool ShowPrimaryColor,
    bool ShowSecondaryColor,
    bool ShowTertiaryColor,
    bool ShowBrightness,
    bool ShowDuration,
    bool ShowCycle,
    bool ShowStep);

public sealed record BackgroundOptionVisibilityInput(
    bool ArduinoAvailable,
    bool BackgroundEnabled,
    bool AlexaAvailable,
    bool AlexaEnabled,
    bool AlexaTurnOffAfterEvent,
    LightPattern Pattern);

public sealed record BackgroundOptionVisibility(
    bool ShowAlexaControls,
    bool ShowAlexaUnavailable,
    bool ShowAlexaEvents,
    bool ShowArduinoEnabled,
    bool ShowArduinoBackground,
    bool ShowColorOptions,
    bool ShowBrightness,
    bool ShowPrimaryColor,
    bool ShowSecondaryColor,
    bool ShowTertiaryColor,
    bool ShowCycle,
    bool ShowStep);
