using NeoTwitch.Models;
using NeoTwitch.Services.Lights;
using NeoTwitch.Services.Text;
using static NeoTwitch.Services.InputValueParser;

namespace NeoTwitch.Services.Alerts;

public static class RuleEditorFormService
{
    public static void Apply(
        EventRule rule,
        RuleEditorFormValues values,
        IEnumerable<AudioAssetConfig> audioLibrary,
        IUiTextService text)
    {
        rule.IsEnabled = values.IsEnabled;
        rule.Name = RuleEditorValueService.ResolveRuleName(values.RuleNameText, rule.Name, values.EventKind, text);
        rule.EventKind = values.EventKind;
        rule.CustomRewardTitle = values.CustomRewardTitle.Trim();
        rule.ChatCommand = values.ChatCommand.Trim();
        rule.MinimumBits = ParseInt(values.MinimumBitsText, 1, 1, 1_000_000);
        rule.SendChatMessage = values.SendChatMessage;
        rule.ChatMessageTemplate = values.ChatMessageTemplate.Trim();
        rule.SendAlexaEvent = values.SendAlexaEvent;
        rule.SendObsScene = values.SendObsScene;
        rule.ObsSceneName = values.ObsSceneName.Trim();
        rule.ObsSceneDelayMs = ParseInt(values.ObsSceneDelayText, 0, 0, 600000);
        rule.ObsReturnToPreviousScene = values.ObsReturnToPreviousScene;
        rule.ObsReturnDelayMs = ParseInt(values.ObsReturnDelayText, 15000, 0, 600000);
        rule.SendObsMedia = values.SendObsMedia;
        rule.ObsMediaKind = values.ObsMediaKind;
        rule.ObsMediaSourceMode = values.ObsMediaSourceMode;
        rule.ObsMediaAssetId = values.ObsMediaAssetId.Trim();
        rule.ObsMediaGroupId = values.ObsMediaGroupId.Trim();
        rule.ObsMediaDurationMs = ParseInt(values.ObsMediaDurationText, 5000, 250, 600000);
        rule.UseLights = values.UseLights;
        rule.PlayAudio = values.PlayAudio;
        rule.AudioSourceMode = values.AudioSourceMode;
        rule.AudioAssetId = values.AudioAssetId.Trim();
        rule.AudioGroupId = values.AudioGroupId.Trim();
        rule.AudioPath = RuleEditorValueService.ResolveLegacyAudioPath(
            rule.AudioSourceMode,
            rule.AudioAssetId,
            audioLibrary);
        rule.Pattern = values.Pattern;
        rule.TargetPins = string.Join(", ", LightCommand.ParsePins(values.TargetPins));
        rule.PrimaryColor = LightCommand.NormalizeColor(values.PrimaryColor);
        rule.SecondaryColor = LightCommand.NormalizeColor(values.SecondaryColor);
        rule.TertiaryColor = LightCommand.NormalizeColor(values.TertiaryColor);
        rule.Brightness = (int)Math.Round(values.Brightness);
        rule.DurationMs = (int)Math.Round(values.DurationMs);
        rule.CycleMs = (int)Math.Round(values.CycleMs);
        rule.StepMs = (int)Math.Round(values.StepMs);
    }
}

public sealed record RuleEditorFormValues(
    bool IsEnabled,
    string RuleNameText,
    TwitchEventKind EventKind,
    string CustomRewardTitle,
    string ChatCommand,
    string MinimumBitsText,
    bool SendChatMessage,
    string ChatMessageTemplate,
    bool SendAlexaEvent,
    bool SendObsScene,
    string ObsSceneName,
    string ObsSceneDelayText,
    bool ObsReturnToPreviousScene,
    string ObsReturnDelayText,
    bool SendObsMedia,
    ObsMediaKind ObsMediaKind,
    MediaSourceMode ObsMediaSourceMode,
    string ObsMediaAssetId,
    string ObsMediaGroupId,
    string ObsMediaDurationText,
    bool UseLights,
    bool PlayAudio,
    AudioSourceMode AudioSourceMode,
    string AudioAssetId,
    string AudioGroupId,
    LightPattern Pattern,
    string TargetPins,
    string PrimaryColor,
    string SecondaryColor,
    string TertiaryColor,
    double Brightness,
    double DurationMs,
    double CycleMs,
    double StepMs);
