using System.Collections.ObjectModel;
using System.IO;
using NeoTwitch.Models.Library;

namespace NeoTwitch.Services.Library;

public static class LibraryConfigNormalizer
{
    public static ObservableCollection<TGroup> NormalizeGroups<TGroup>(
        ObservableCollection<TGroup>? groups,
        string fallbackName,
        Func<string>? idFactory = null)
        where TGroup : ILibraryGroupConfig
    {
        var createId = idFactory ?? (() => Guid.NewGuid().ToString("N"));
        groups ??= [];

        foreach (var group in groups)
        {
            group.Id = string.IsNullOrWhiteSpace(group.Id) ? createId() : group.Id;
            group.Name = string.IsNullOrWhiteSpace(group.Name) ? fallbackName : group.Name.Trim();
        }

        return groups;
    }

    public static ObservableCollection<TAsset> NormalizeAssets<TAsset>(
        ObservableCollection<TAsset>? library,
        Action<TAsset>? normalizeSpecificFields = null,
        Func<string>? idFactory = null)
        where TAsset : ILibraryAssetConfig
    {
        var createId = idFactory ?? (() => Guid.NewGuid().ToString("N"));
        library ??= [];

        foreach (var asset in library)
        {
            asset.Id = string.IsNullOrWhiteSpace(asset.Id) ? createId() : asset.Id;
            asset.Name = string.IsNullOrWhiteSpace(asset.Name)
                ? Path.GetFileNameWithoutExtension(asset.FilePath ?? "")
                : asset.Name.Trim();
            asset.FilePath ??= "";
            asset.GroupId ??= "";
            normalizeSpecificFields?.Invoke(asset);
        }

        return library;
    }
}
