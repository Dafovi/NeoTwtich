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
        if (!rule.SendObsMedia)
        {
            return null;
        }

        fileExists ??= File.Exists;
        var library = rule.ObsMediaKind == ObsMediaKind.Image
            ? imageLibrary
            : videoLibrary;

        if (rule.ObsMediaSourceMode == MediaSourceMode.Group)
        {
            var candidates = library
                .Where(asset => string.Equals(asset.GroupId, rule.ObsMediaGroupId, StringComparison.OrdinalIgnoreCase))
                .Where(asset => fileExists(asset.FilePath))
                .ToArray();

            return candidates.Length == 0
                ? null
                : candidates[random.Next(candidates.Length)];
        }

        return library
            .Where(asset => string.Equals(asset.Id, rule.ObsMediaAssetId, StringComparison.OrdinalIgnoreCase))
            .FirstOrDefault(asset => fileExists(asset.FilePath));
    }

    public static TimeSpan ResolveRuleMediaDuration(EventRule rule, MediaAssetConfig asset)
    {
        if (rule.ObsMediaKind == ObsMediaKind.Video)
        {
            return TimeSpan.FromMilliseconds(asset.DurationMs > 0 ? asset.DurationMs : 5000);
        }

        return TimeSpan.FromMilliseconds(Math.Clamp(
            rule.ObsMediaDurationMs,
            ApplicationLimits.MinAlertDurationMs,
            ApplicationLimits.MaxAlertDurationMs));
    }
}
