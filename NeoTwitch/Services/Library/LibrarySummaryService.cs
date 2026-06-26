using NeoTwitch.Models.Library;

namespace NeoTwitch.Services.Library;

public readonly record struct LibrarySummaryDisplay(
    string AssetCountText,
    string GroupCountText,
    string LastAssetText,
    string FooterText);

public static class LibrarySummaryService
{
    public static LibrarySummaryDisplay Create<TAsset, TGroup>(
        IReadOnlyCollection<TAsset> assets,
        IReadOnlyCollection<TGroup> groups,
        int visibleCount,
        string groupFilterId,
        IReadOnlyDictionary<string, string> groupsById,
        string footerNoun,
        string lastUnusedText,
        string selectedGroupText)
        where TAsset : ILibraryAssetConfig
        where TGroup : ILibraryGroupConfig
    {
        var lastAsset = assets
            .Where(asset => asset.LastUsedAt is not null)
            .OrderByDescending(asset => asset.LastUsedAt)
            .FirstOrDefault();

        var groupFilterText = string.IsNullOrWhiteSpace(groupFilterId)
            ? ""
            : $" del grupo {groupsById.GetValueOrDefault(groupFilterId, selectedGroupText)}";

        return new LibrarySummaryDisplay(
            assets.Count.ToString(),
            groups.Count.ToString(),
            lastAsset?.DisplayName ?? lastUnusedText,
            $"Mostrando {visibleCount} de {assets.Count} {footerNoun}{groupFilterText}");
    }
}
