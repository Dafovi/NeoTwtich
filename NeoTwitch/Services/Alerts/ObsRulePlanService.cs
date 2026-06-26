using NeoTwitch.Models;
using NeoTwitch.ViewModels.Obs;

namespace NeoTwitch.Services.Alerts;

public static class ObsRulePlanService
{
    public static bool ShouldSendScene(EventRule rule, bool obsConfigured)
    {
        return obsConfigured
            && rule.SendObsScene
            && !string.IsNullOrWhiteSpace(rule.ObsSceneName);
    }

    public static string ResolveTargetScene(EventRule rule)
    {
        return rule.ObsSceneName.Trim();
    }

    public static ObsSceneRestoreRequest? BuildSceneRestoreRequest(
        EventRule rule,
        string? previousScene,
        string targetScene,
        DateTimeOffset startedAt)
    {
        if (!rule.ObsReturnToPreviousScene
            || string.IsNullOrWhiteSpace(previousScene)
            || string.Equals(previousScene.Trim(), targetScene.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return new ObsSceneRestoreRequest(
            previousScene.Trim(),
            targetScene.Trim(),
            TimeSpan.FromMilliseconds(Math.Clamp(rule.ObsReturnDelayMs, 0, ApplicationLimits.MaxAlertDurationMs)),
            startedAt);
    }

    public static bool ShouldSendMedia(EventRule rule, bool obsConfigured)
    {
        return obsConfigured && rule.SendObsMedia;
    }

    public static string ResolveMediaSceneName(EventRule rule, string? currentScene)
    {
        return rule.SendObsScene && !string.IsNullOrWhiteSpace(rule.ObsSceneName)
            ? rule.ObsSceneName.Trim()
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
}
