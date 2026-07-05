using NeoTwitch.Models;
using NeoTwitch.Services.Lights;

namespace NeoTwitch.Services.Ui;

public static class OptionVisibilityService
{
    public static RuleOptionVisibility ResolveRule(RuleOptionVisibilityInput input)
    {
        var useLights = input.ArduinoAvailable && input.UseLights;
        var usesAnyLightColor = useLights
            && (LightPatternCapabilities.UsesPrimaryColor(input.Pattern)
                || LightPatternCapabilities.UsesSecondaryColor(input.Pattern)
                || LightPatternCapabilities.UsesTertiaryColor(input.Pattern));

        return new RuleOptionVisibility(
            ShowRewardTitle: input.EventKind == TwitchEventKind.ChannelPointRedemption,
            ShowChatCommand: input.EventKind == TwitchEventKind.ChatCommand,
            ShowMinimumBits: input.EventKind == TwitchEventKind.Cheer,
            ShowAudioDetails: input.PlayAudio,
            ShowAudioSingle: input.PlayAudio && input.AudioSourceMode == AudioSourceMode.Single && input.HasAudioAssets,
            ShowAudioGroup: input.PlayAudio && input.AudioSourceMode == AudioSourceMode.Group && input.HasAudioGroups,
            ShowAudioEmptyHint: input.PlayAudio
                && ((input.AudioSourceMode == AudioSourceMode.Single && !input.HasAudioAssets)
                    || (input.AudioSourceMode == AudioSourceMode.Group && !input.HasAudioGroups)),
            ShowChatDetails: input.SendChatMessage,
            ShowLightsAction: input.ArduinoAvailable,
            ShowAlexaAction: input.AlexaAvailable,
            ShowAlexaDetails: input.AlexaAvailable && input.SendAlexaEvent,
            ShowObsAction: input.ObsAvailable,
            ShowObsDetails: input.ObsAvailable,
            ShowObsSceneDetails: input.ObsAvailable && input.SendObsScene,
            ShowObsSceneTiming: input.ObsAvailable && input.SendObsScene && !string.IsNullOrWhiteSpace(input.SelectedObsSceneName),
            ShowObsReturnDelay: input.ObsAvailable && input.SendObsScene && !input.SendObsMedia && input.ReturnObsScene,
            ShowObsEmptyHint: input.ObsAvailable && input.SendObsScene && !input.HasObsScenes,
            ShowObsMediaDetails: input.ObsAvailable && input.SendObsMedia,
            ShowObsMediaDuration: input.ObsAvailable && input.SendObsMedia && input.ObsMediaKind == ObsMediaKind.Image,
            ShowObsMediaAsset: input.ObsAvailable && input.SendObsMedia && input.ObsMediaSourceMode == MediaSourceMode.Single && input.HasObsMediaAssets,
            ShowObsMediaGroup: input.ObsAvailable && input.SendObsMedia && input.ObsMediaSourceMode == MediaSourceMode.Group && input.HasObsMediaGroups,
            ShowObsMediaEmptyHint: input.ObsAvailable
                && input.SendObsMedia
                && ((input.ObsMediaSourceMode == MediaSourceMode.Single && !input.HasObsMediaAssets)
                    || (input.ObsMediaSourceMode == MediaSourceMode.Group && !input.HasObsMediaGroups)),
            ShowLightConfiguration: useLights,
            ShowLightColorOptions: usesAnyLightColor,
            ShowPrimaryColor: useLights && LightPatternCapabilities.UsesPrimaryColor(input.Pattern),
            ShowSecondaryColor: useLights && LightPatternCapabilities.UsesSecondaryColor(input.Pattern),
            ShowTertiaryColor: useLights && LightPatternCapabilities.UsesTertiaryColor(input.Pattern),
            ShowBrightness: useLights && LightPatternCapabilities.UsesBrightness(input.Pattern),
            ShowDuration: useLights && !input.PlayAudio,
            ShowCycle: useLights && LightPatternCapabilities.UsesCycle(input.Pattern),
            ShowStep: useLights && LightPatternCapabilities.UsesStep(input.Pattern));
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
    bool SendObsMedia,
    ObsMediaKind ObsMediaKind,
    MediaSourceMode ObsMediaSourceMode,
    bool HasObsMediaAssets,
    bool HasObsMediaGroups,
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
    bool ShowAlexaAction,
    bool ShowAlexaDetails,
    bool ShowObsAction,
    bool ShowObsDetails,
    bool ShowObsSceneDetails,
    bool ShowObsSceneTiming,
    bool ShowObsReturnDelay,
    bool ShowObsEmptyHint,
    bool ShowObsMediaDetails,
    bool ShowObsMediaDuration,
    bool ShowObsMediaAsset,
    bool ShowObsMediaGroup,
    bool ShowObsMediaEmptyHint,
    bool ShowLightConfiguration,
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
