namespace NeoTwitch.Models;

public sealed record AlertExecutionRuleSnapshot(
    string RuleId,
    string RuleName,
    TwitchEventKind EventKind,
    AlertAudioActionSnapshot Audio,
    AlertChatActionSnapshot Chat,
    AlertAlexaActionSnapshot Alexa,
    AlertObsActionSnapshot Obs,
    AlertLightActionSnapshot Lights,
    AlertVirtualLightActionSnapshot VirtualLights);

public sealed record AlertAudioActionSnapshot(
    bool Enabled,
    AudioSourceMode SourceMode,
    string AssetId,
    string GroupId,
    string LegacyPath);

public sealed record AlertChatActionSnapshot(bool Enabled, string MessageTemplate);

public sealed record AlertAlexaActionSnapshot(bool Enabled, string EventName);

public sealed record AlertObsActionSnapshot(
    AlertObsSceneActionSnapshot Scene,
    AlertObsMediaActionSnapshot Image,
    AlertObsMediaActionSnapshot Video);

public sealed record AlertObsSceneActionSnapshot(
    bool Enabled,
    string SceneName,
    int DelayMs,
    bool ReturnToPreviousScene,
    int ReturnDelayMs);

public sealed record AlertObsMediaActionSnapshot(
    bool Enabled,
    ObsMediaKind Kind,
    MediaSourceMode SourceMode,
    string AssetId,
    string GroupId,
    int DurationMs);

public sealed record AlertLightActionSnapshot(
    bool Enabled,
    LightPattern Pattern,
    string TargetPins,
    string PrimaryColor,
    string SecondaryColor,
    string TertiaryColor,
    int Brightness,
    int DurationMs,
    int CycleMs,
    int StepMs);

public sealed record AlertVirtualLightActionSnapshot(
    bool Enabled,
    bool ToObs,
    bool ToScreen,
    string ScreenId,
    LightPattern Pattern,
    string PrimaryColor,
    string SecondaryColor,
    string TertiaryColor,
    int Brightness,
    int DurationMs,
    int CycleMs,
    int StepMs,
    int ObsOpacity,
    int ScreenPixelSize,
    int ScreenSaturation);

public sealed record AlertTriggerSnapshot(
    TwitchEventKind Kind,
    string Title,
    string UserName,
    string RewardTitle,
    int? ViewerCount,
    int? Bits,
    string Message,
    string RawType,
    string EventSubMessageId,
    string EventSubSessionId,
    string EventSubMessageType)
{
    public TwitchEvent ToTwitchEvent() => new()
    {
        Kind = Kind,
        Title = Title,
        UserName = UserName,
        RewardTitle = RewardTitle,
        ViewerCount = ViewerCount,
        Bits = Bits,
        Message = Message,
        RawType = RawType,
        EventSubMessageId = EventSubMessageId,
        EventSubSessionId = EventSubSessionId,
        EventSubMessageType = EventSubMessageType
    };
}

public static class AlertExecutionSnapshotFactory
{
    public static AlertExecutionRuleSnapshot Create(EventRule rule) => new(
        rule.Id,
        rule.Name,
        rule.EventKind,
        new AlertAudioActionSnapshot(
            rule.PlayAudio,
            rule.AudioSourceMode,
            rule.AudioAssetId,
            rule.AudioGroupId,
            rule.AudioPath),
        new AlertChatActionSnapshot(rule.SendChatMessage, rule.ChatMessageTemplate),
        new AlertAlexaActionSnapshot(rule.SendAlexaEvent, rule.AlexaEventName),
        new AlertObsActionSnapshot(
            new AlertObsSceneActionSnapshot(
                rule.SendObsScene,
                rule.ObsSceneName,
                rule.ObsSceneDelayMs,
                rule.ObsReturnToPreviousScene,
                rule.ObsReturnDelayMs),
            new AlertObsMediaActionSnapshot(
                rule.SendObsImage,
                ObsMediaKind.Image,
                rule.ObsImageSourceMode,
                rule.ObsImageAssetId,
                rule.ObsImageGroupId,
                rule.ObsImageDurationMs),
            new AlertObsMediaActionSnapshot(
                rule.SendObsVideo,
                ObsMediaKind.Video,
                rule.ObsVideoSourceMode,
                rule.ObsVideoAssetId,
                rule.ObsVideoGroupId,
                rule.ObsMediaDurationMs)),
        new AlertLightActionSnapshot(
            rule.UseLights,
            rule.Pattern,
            rule.TargetPins,
            rule.PrimaryColor,
            rule.SecondaryColor,
            rule.TertiaryColor,
            rule.Brightness,
            rule.DurationMs,
            rule.CycleMs,
            rule.StepMs),
        new AlertVirtualLightActionSnapshot(
            rule.UseVirtualLights,
            rule.VirtualLightsToObs,
            rule.VirtualLightsToScreen,
            rule.VirtualLightsScreenId,
            rule.VirtualLightsPattern,
            rule.VirtualLightsPrimaryColor,
            rule.VirtualLightsSecondaryColor,
            rule.VirtualLightsTertiaryColor,
            rule.VirtualLightsBrightness,
            rule.VirtualLightsDurationMs,
            rule.VirtualLightsCycleMs,
            rule.VirtualLightsStepMs,
            rule.VirtualLightsObsOpacity,
            rule.VirtualLightsScreenPixelSize,
            rule.VirtualLightsScreenSaturation));

    public static AlertTriggerSnapshot Create(TwitchEvent twitchEvent) => new(
        twitchEvent.Kind,
        twitchEvent.Title,
        twitchEvent.UserName ?? "",
        twitchEvent.RewardTitle ?? "",
        twitchEvent.ViewerCount,
        twitchEvent.Bits,
        twitchEvent.Message ?? "",
        twitchEvent.RawType ?? "",
        twitchEvent.EventSubMessageId,
        twitchEvent.EventSubSessionId,
        twitchEvent.EventSubMessageType);
}
