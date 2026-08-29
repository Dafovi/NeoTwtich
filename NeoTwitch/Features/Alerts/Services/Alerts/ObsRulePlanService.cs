using NeoTwitch.Models;
using NeoTwitch.Services.Library;
using NeoTwitch.ViewModels.Obs;

namespace NeoTwitch.Services.Alerts;

public enum ObsRuleMediaPlanStatus
{
    Disabled,
    MissingAsset,
    MissingScene,
    Ready
}

public sealed record ObsRuleMediaExecutionPlan(
    ObsRuleMediaPlanStatus Status,
    MediaAssetConfig? Asset,
    string SceneName,
    string SourceName,
    TimeSpan Duration,
    int? VolumePercent)
{
    public bool IsReady => Status == ObsRuleMediaPlanStatus.Ready;
}

public static class ObsRulePlanService
{
    public static bool ShouldSendScene(EventRule rule, bool obsConfigured)
    {
        return ShouldSendScene(AlertExecutionSnapshotFactory.Create(rule), obsConfigured);
    }

    public static bool ShouldSendScene(AlertExecutionRuleSnapshot rule, bool obsConfigured)
    {
        return obsConfigured
            && rule.Obs.Scene.Enabled
            && !string.IsNullOrWhiteSpace(rule.Obs.Scene.SceneName);
    }

    public static string ResolveTargetScene(EventRule rule)
    {
        return ResolveTargetScene(AlertExecutionSnapshotFactory.Create(rule));
    }

    public static string ResolveTargetScene(AlertExecutionRuleSnapshot rule)
    {
        return rule.Obs.Scene.SceneName.Trim();
    }

    public static ObsSceneRestoreRequest? BuildSceneRestoreRequest(
        EventRule rule,
        string? previousScene,
        string targetScene,
        DateTimeOffset startedAt)
    {
        return BuildSceneRestoreRequest(AlertExecutionSnapshotFactory.Create(rule), previousScene, targetScene, startedAt);
    }

    public static ObsSceneRestoreRequest? BuildSceneRestoreRequest(
        AlertExecutionRuleSnapshot rule,
        string? previousScene,
        string targetScene,
        DateTimeOffset startedAt)
    {
        if (!rule.Obs.Scene.ReturnToPreviousScene
            || string.IsNullOrWhiteSpace(previousScene)
            || string.Equals(previousScene.Trim(), targetScene.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return new ObsSceneRestoreRequest(
            previousScene.Trim(),
            targetScene.Trim(),
            TimeSpan.FromMilliseconds(Math.Clamp(rule.Obs.Scene.ReturnDelayMs, 0, ApplicationLimits.MaxAlertDurationMs)),
            startedAt);
    }

    public static bool ShouldSendMedia(EventRule rule, bool obsConfigured)
    {
        return obsConfigured && rule.SendObsMedia;
    }

    public static bool ShouldSendMedia(bool obsConfigured, bool sendMedia)
    {
        return obsConfigured && sendMedia;
    }

    public static string ResolveMediaSceneName(EventRule rule, string? currentScene)
    {
        return ResolveMediaSceneName(AlertExecutionSnapshotFactory.Create(rule), currentScene);
    }

    public static string ResolveMediaSceneName(AlertExecutionRuleSnapshot rule, string? currentScene)
    {
        return rule.Obs.Scene.Enabled && !string.IsNullOrWhiteSpace(rule.Obs.Scene.SceneName)
            ? rule.Obs.Scene.SceneName.Trim()
            : (currentScene ?? string.Empty).Trim();
    }

    public static string ResolveAlertSourceName(
        ObsMediaKind mediaKind,
        string imageSourceName,
        string videoSourceName)
    {
        return mediaKind == ObsMediaKind.Image
            ? imageSourceName
            : videoSourceName;
    }

    public static ObsRuleMediaExecutionPlan BuildMediaExecutionPlan(
        AlertExecutionRuleSnapshot rule,
        AppConfig config,
        string? currentScene,
        MediaAssetConfig? asset,
        bool sendMedia,
        ObsMediaKind mediaKind,
        int durationMs,
        string imageSourceName,
        string videoSourceName)
    {
        if (!ShouldSendMedia(config.Obs.IsConfigured, sendMedia))
        {
            return new ObsRuleMediaExecutionPlan(
                ObsRuleMediaPlanStatus.Disabled,
                null,
                "",
                "",
                TimeSpan.Zero,
                null);
        }

        if (asset is null)
        {
            return new ObsRuleMediaExecutionPlan(
                ObsRuleMediaPlanStatus.MissingAsset,
                null,
                "",
                "",
                TimeSpan.Zero,
                null);
        }

        var sceneName = ResolveMediaSceneName(rule, currentScene);
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            return new ObsRuleMediaExecutionPlan(
                ObsRuleMediaPlanStatus.MissingScene,
                asset,
                "",
                "",
                TimeSpan.Zero,
                null);
        }

        return new ObsRuleMediaExecutionPlan(
            ObsRuleMediaPlanStatus.Ready,
            asset,
            sceneName,
            ResolveAlertSourceName(mediaKind, imageSourceName, videoSourceName),
            MediaRuleAssetService.ResolveRuleMediaDuration(mediaKind, durationMs, asset),
            mediaKind == ObsMediaKind.Video ? config.VideoVolumePercent : null);
    }

    public static ObsRuleMediaExecutionPlan BuildMediaExecutionPlan(
        EventRule rule,
        AppConfig config,
        string? currentScene,
        MediaAssetConfig? asset,
        string imageSourceName,
        string videoSourceName)
    {
        return BuildMediaExecutionPlan(
            rule,
            config,
            currentScene,
            asset,
            rule.SendObsMedia,
            rule.ObsMediaKind,
            rule.ObsMediaDurationMs,
            imageSourceName,
            videoSourceName);
    }

    public static ObsRuleMediaExecutionPlan BuildMediaExecutionPlan(
        EventRule rule,
        AppConfig config,
        string? currentScene,
        MediaAssetConfig? asset,
        bool sendMedia,
        ObsMediaKind mediaKind,
        int durationMs,
        string imageSourceName,
        string videoSourceName)
    {
        if (!ShouldSendMedia(config.Obs.IsConfigured, sendMedia))
        {
            return new ObsRuleMediaExecutionPlan(
                ObsRuleMediaPlanStatus.Disabled,
                null,
                "",
                "",
                TimeSpan.Zero,
                null);
        }

        if (asset is null)
        {
            return new ObsRuleMediaExecutionPlan(
                ObsRuleMediaPlanStatus.MissingAsset,
                null,
                "",
                "",
                TimeSpan.Zero,
                null);
        }

        var sceneName = ResolveMediaSceneName(rule, currentScene);
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            return new ObsRuleMediaExecutionPlan(
                ObsRuleMediaPlanStatus.MissingScene,
                asset,
                "",
                "",
                TimeSpan.Zero,
                null);
        }

        return new ObsRuleMediaExecutionPlan(
            ObsRuleMediaPlanStatus.Ready,
            asset,
            sceneName,
            ResolveAlertSourceName(mediaKind, imageSourceName, videoSourceName),
            MediaRuleAssetService.ResolveRuleMediaDuration(mediaKind, durationMs, asset),
            mediaKind == ObsMediaKind.Video ? config.VideoVolumePercent : null);
    }

    public static ObsMediaHideRequest BuildMediaHideRequest(
        string sceneName,
        string sourceName,
        TimeSpan duration,
        DateTimeOffset startedAt)
    {
        return new ObsMediaHideRequest(
            sceneName.Trim(),
            sourceName.Trim(),
            duration,
            startedAt);
    }

    public static ObsSceneRestoreRequest? AlignSceneRestoreWithMedia(
        ObsSceneRestoreRequest? restore,
        ObsMediaHideRequest? mediaHide)
    {
        return restore is not null && mediaHide is not null
            ? restore with { Delay = mediaHide.Duration, StartedAt = mediaHide.StartedAt }
            : restore;
    }

    public static ObsSceneRestoreRequest? AlignSceneRestoreWithMedia(
        ObsSceneRestoreRequest? restore,
        IEnumerable<ObsMediaHideRequest> mediaHides)
    {
        var longestMedia = mediaHides
            .OrderByDescending(media => media.Duration)
            .FirstOrDefault();

        return AlignSceneRestoreWithMedia(restore, longestMedia);
    }
}
