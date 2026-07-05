using NeoTwitch.Models.Library;

namespace NeoTwitch.Services.Library;

public readonly record struct LibrarySummaryDisplay(
    string AssetCountText,
    string GroupCountText,
    string LastAssetText,
    string FooterText);

public readonly record struct LibrarySummaryLabels(
    string FooterFormat,
    string GroupFilterFormat,
    string LastUnusedText,
    string SelectedGroupFallbackText);

public static class LibrarySummaryService
{
    public static LibrarySummaryDisplay Create<TAsset, TGroup>(
        IReadOnlyCollection<TAsset> assets,
        IReadOnlyCollection<TGroup> groups,
        int visibleCount,
        string groupFilterId,
        IReadOnlyDictionary<string, string> groupsById,
        string footerNoun,
        LibrarySummaryLabels labels)
        where TAsset : ILibraryAssetConfig
        where TGroup : ILibraryGroupConfig
    {
        var lastAsset = assets
            .Where(asset => asset.LastUsedAt is not null)
            .OrderByDescending(asset => asset.LastUsedAt)
            .FirstOrDefault();

        var groupFilterText = string.IsNullOrWhiteSpace(groupFilterId)
            ? ""
            : string.Format(labels.GroupFilterFormat, groupsById.GetValueOrDefault(groupFilterId, labels.SelectedGroupFallbackText));

        return new LibrarySummaryDisplay(
            assets.Count.ToString(),
            groups.Count.ToString(),
            lastAsset?.DisplayName ?? labels.LastUnusedText,
            string.Format(labels.FooterFormat, visibleCount, assets.Count, footerNoun, groupFilterText));
    }
}
