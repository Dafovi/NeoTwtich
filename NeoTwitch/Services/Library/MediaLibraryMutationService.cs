using NeoTwitch.Models;
using NeoTwitch.ViewModels.Library;

namespace NeoTwitch.Services.Library;

public sealed record MediaAssetRemovalResult(MediaAssetConfig? RemovedAsset, int UpdatedRuleCount)
{
    public bool Removed => RemovedAsset is not null;
}

public static class MediaLibraryMutationService
{
    public static MediaAssetRemovalResult RemoveMediaAsset(AppConfig config, MediaLibraryKind kind, string assetId)
    {
        var library = kind == MediaLibraryKind.Image ? config.ImageLibrary : config.VideoLibrary;
        var asset = library.FirstOrDefault(item => string.Equals(item.Id, assetId, StringComparison.OrdinalIgnoreCase));
        if (asset is null)
        {
            return new MediaAssetRemovalResult(null, 0);
        }

        library.Remove(asset);

        var obsKind = MediaLibraryKindCatalog.Get(kind).ObsKind;
        var updatedRules = 0;
        foreach (var rule in config.Rules.Where(rule =>
            rule.ObsMediaKind == obsKind
            && rule.ObsMediaSourceMode == MediaSourceMode.Single
            && string.Equals(rule.ObsMediaAssetId, asset.Id, StringComparison.OrdinalIgnoreCase)))
        {
            rule.ObsMediaAssetId = "";
            rule.SendObsMedia = false;
            updatedRules++;
        }

        return new MediaAssetRemovalResult(asset, updatedRules);
    }
}
