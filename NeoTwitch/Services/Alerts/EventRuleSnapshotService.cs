using NeoTwitch.Models;
using NeoTwitch.Services.Text;

namespace NeoTwitch.Services.Alerts;

public static class EventRuleSnapshotService
{
    public static EventRule Clone(EventRule rule)
    {
        var clone = new EventRule();
        CopyValues(rule, clone);
        return clone;
    }

    public static EventRule Duplicate(EventRule rule, IUiTextService text)
    {
        var copy = Clone(rule);
        copy.Id = Guid.NewGuid().ToString("N");
        copy.Name = $"{rule.Name} {text.Get(UiTextKeys.ConfigurationCopySuffix)}".Trim();
        return copy;
    }

    public static void CopyValues(EventRule source, EventRule target)
    {
        target.Id = source.Id;
        target.Name = source.Name;
        target.IsEnabled = source.IsEnabled;
        target.EventKind = source.EventKind;
        target.CustomRewardTitle = source.CustomRewardTitle;
        target.ChatCommand = source.ChatCommand;
        target.MinimumBits = source.MinimumBits;
        target.UseLights = source.UseLights;
        target.PlayAudio = source.PlayAudio;
        target.AudioPath = source.AudioPath;
        target.AudioSourceMode = source.AudioSourceMode;
        target.AudioAssetId = source.AudioAssetId;
        target.AudioGroupId = source.AudioGroupId;
        target.SendChatMessage = source.SendChatMessage;
        target.ChatMessageTemplate = source.ChatMessageTemplate;
        target.SendAlexaEvent = source.SendAlexaEvent;
        target.AlexaEventName = source.AlexaEventName;
        target.SendObsScene = source.SendObsScene;
        target.ObsSceneName = source.ObsSceneName;
        target.ObsSceneDelayMs = source.ObsSceneDelayMs;
        target.ObsReturnToPreviousScene = source.ObsReturnToPreviousScene;
        target.ObsReturnDelayMs = source.ObsReturnDelayMs;
        target.SendObsMedia = source.SendObsMedia;
        target.ObsMediaKind = source.ObsMediaKind;
        target.ObsMediaSourceMode = source.ObsMediaSourceMode;
        target.ObsMediaAssetId = source.ObsMediaAssetId;
        target.ObsMediaGroupId = source.ObsMediaGroupId;
        target.ObsMediaDurationMs = source.ObsMediaDurationMs;
        target.Pattern = source.Pattern;
        target.TargetPins = source.TargetPins;
        target.PrimaryColor = source.PrimaryColor;
        target.SecondaryColor = source.SecondaryColor;
        target.TertiaryColor = source.TertiaryColor;
        target.Brightness = source.Brightness;
        target.DurationMs = source.DurationMs;
        target.CycleMs = source.CycleMs;
        target.StepMs = source.StepMs;
        target.LightsActionAvailable = source.LightsActionAvailable;
        target.AlexaActionAvailable = source.AlexaActionAvailable;
        target.ObsActionAvailable = source.ObsActionAvailable;
    }

    public static bool HaveSameEditableValues(EventRule left, EventRule right)
    {
        return left.Name == right.Name
            && left.IsEnabled == right.IsEnabled
            && left.EventKind == right.EventKind
            && left.CustomRewardTitle == right.CustomRewardTitle
            && left.ChatCommand == right.ChatCommand
            && left.MinimumBits == right.MinimumBits
            && left.UseLights == right.UseLights
            && left.PlayAudio == right.PlayAudio
            && left.AudioPath == right.AudioPath
            && left.AudioSourceMode == right.AudioSourceMode
            && left.AudioAssetId == right.AudioAssetId
            && left.AudioGroupId == right.AudioGroupId
            && left.SendChatMessage == right.SendChatMessage
            && left.ChatMessageTemplate == right.ChatMessageTemplate
            && left.SendAlexaEvent == right.SendAlexaEvent
            && left.AlexaEventName == right.AlexaEventName
            && left.SendObsScene == right.SendObsScene
            && left.ObsSceneName == right.ObsSceneName
            && left.ObsSceneDelayMs == right.ObsSceneDelayMs
            && left.ObsReturnToPreviousScene == right.ObsReturnToPreviousScene
            && left.ObsReturnDelayMs == right.ObsReturnDelayMs
            && left.SendObsMedia == right.SendObsMedia
            && left.ObsMediaKind == right.ObsMediaKind
            && left.ObsMediaSourceMode == right.ObsMediaSourceMode
            && left.ObsMediaAssetId == right.ObsMediaAssetId
            && left.ObsMediaGroupId == right.ObsMediaGroupId
            && left.ObsMediaDurationMs == right.ObsMediaDurationMs
            && left.Pattern == right.Pattern
            && left.TargetPins == right.TargetPins
            && left.PrimaryColor == right.PrimaryColor
            && left.SecondaryColor == right.SecondaryColor
            && left.TertiaryColor == right.TertiaryColor
            && left.Brightness == right.Brightness
            && left.DurationMs == right.DurationMs
            && left.CycleMs == right.CycleMs
            && left.StepMs == right.StepMs;
    }
}
