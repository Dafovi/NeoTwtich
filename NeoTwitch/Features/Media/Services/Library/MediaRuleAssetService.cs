using System.IO;
using NeoTwitch.Models;

namespace NeoTwitch.Services.Library;

public static class MediaRuleAssetService
{
    public static MediaAssetConfig? ResolveRuleMediaAsset(
        EventRule rule,
        IReadOnlyCollection<MediaAssetConfig> imageLibrary,
        IReadOnlyCollection<MediaAssetConfig> videoLibrary,
        Random random,
        Func<string, bool>? fileExists = null)
    {
        return ResolveRuleMediaAsset(
            rule.SendObsMedia,
            rule.ObsMediaKind,
            rule.ObsMediaSourceMode,
            rule.ObsMediaAssetId,
            rule.ObsMediaGroupId,
            imageLibrary,
            videoLibrary,
            random,
            fileExists);
    }

    public static MediaAssetConfig? ResolveRuleMediaAsset(
        bool shouldSend,
        ObsMediaKind kind,
        MediaSourceMode sourceMode,
        string assetId,
        string groupId,
        IReadOnlyCollection<MediaAssetConfig> imageLibrary,
        IReadOnlyCollection<MediaAssetConfig> videoLibrary,
        Random random,
        Func<string, bool>? fileExists = null)
    {
        if (!shouldSend)
        {
            return null;
        }

        fileExists ??= File.Exists;
        var library = kind == ObsMediaKind.Image
            ? imageLibrary
            : videoLibrary;

        if (sourceMode == MediaSourceMode.Group)
        {
            var candidates = library
                .Where(asset => string.Equals(asset.GroupId, groupId, StringComparison.OrdinalIgnoreCase))
                .Where(asset => fileExists(asset.FilePath))
                .ToArray();

            return candidates.Length == 0
                ? null
                : candidates[random.Next(candidates.Length)];
        }

        return library
            .Where(asset => string.Equals(asset.Id, assetId, StringComparison.OrdinalIgnoreCase))
            .FirstOrDefault(asset => fileExists(asset.FilePath));
    }

    public static TimeSpan ResolveRuleMediaDuration(EventRule rule, MediaAssetConfig asset)
    {
        return ResolveRuleMediaDuration(rule.ObsMediaKind, rule.ObsMediaDurationMs, asset);
    }

    public static TimeSpan ResolveRuleMediaDuration(ObsMediaKind kind, int durationMs, MediaAssetConfig asset)
    {
        return kind == ObsMediaKind.Video
            ? TimeSpan.FromMilliseconds(asset.DurationMs > 0 ? asset.DurationMs : 5000)
            : TimeSpan.FromMilliseconds(Math.Clamp(
                durationMs,
                ApplicationLimits.MinAlertDurationMs,
                ApplicationLimits.MaxAlertDurationMs));
    }
}
