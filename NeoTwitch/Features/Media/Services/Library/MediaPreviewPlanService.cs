using NeoTwitch.Models;
using NeoTwitch.ViewModels.Library;

namespace NeoTwitch.Services.Library;

public sealed record MediaPreviewPlan(
    string SceneName,
    ObsMediaKind ObsKind,
    string SourceName,
    TimeSpan Duration,
    int? VolumePercent);

public static class MediaPreviewPlanService
{
    public static MediaPreviewPlan? Build(
        MediaLibraryKind kind,
        MediaAssetConfig asset,
        string? currentScene,
        int videoVolumePercent)
    {
        if (string.IsNullOrWhiteSpace(currentScene))
        {
            return null;
        }

        var info = MediaLibraryKindCatalog.Get(kind);
        var duration = info.ObsKind == ObsMediaKind.Video
            ? TimeSpan.FromMilliseconds(asset.DurationMs > 0 ? asset.DurationMs : 5000)
            : TimeSpan.FromSeconds(5);

        return new MediaPreviewPlan(
            currentScene.Trim(),
            info.ObsKind,
            info.PreviewSourceName,
            duration,
            info.ObsKind == ObsMediaKind.Video ? videoVolumePercent : null);
    }
}
