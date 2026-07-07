using NeoTwitch.Models;
using NeoTwitch.Models.Library;

namespace NeoTwitch.Services.Library;

public static class LibraryGroupService
{
    public static LibraryGroupMutation<TGroup> GetOrCreate<TGroup>(
        ICollection<TGroup> groups,
        string? rawName)
        where TGroup : class, ILibraryGroupConfig, new()
    {
        var name = rawName?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(name))
        {
            return LibraryGroupMutation<TGroup>.Invalid();
        }

        var existing = groups.FirstOrDefault(group => string.Equals(group.Name, name, StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
        {
            return LibraryGroupMutation<TGroup>.Existing(existing);
        }

        var group = new TGroup { Name = name };
        groups.Add(group);
        return LibraryGroupMutation<TGroup>.NewlyCreated(group);
    }

    public static int CountAssetsInGroup<TAsset>(IEnumerable<TAsset> library, string groupId)
        where TAsset : ILibraryAssetConfig
    {
        return library.Count(asset => string.Equals(asset.GroupId, groupId, StringComparison.OrdinalIgnoreCase));
    }

    public static int ClearGroupFromAssets<TAsset>(IEnumerable<TAsset> library, string groupId)
        where TAsset : ILibraryAssetConfig
    {
        var cleared = 0;
        foreach (var asset in library.Where(asset => string.Equals(asset.GroupId, groupId, StringComparison.OrdinalIgnoreCase)))
        {
            asset.GroupId = "";
            cleared++;
        }

        return cleared;
    }

    public static int ClearAudioGroupFromRules(IEnumerable<EventRule> rules, string groupId)
    {
        var cleared = 0;
        foreach (var rule in rules.Where(rule => rule.AudioSourceMode == AudioSourceMode.Group
                     && string.Equals(rule.AudioGroupId, groupId, StringComparison.OrdinalIgnoreCase)))
        {
            rule.AudioGroupId = "";
            rule.PlayAudio = false;
            cleared++;
        }

        return cleared;
    }

    public static int ClearMediaGroupFromRules(IEnumerable<EventRule> rules, ObsMediaKind kind, string groupId)
    {
        var cleared = 0;
        foreach (var rule in rules)
        {
            var updated = false;
            if (rule.ObsMediaKind == kind
                && rule.ObsMediaSourceMode == MediaSourceMode.Group
                && string.Equals(rule.ObsMediaGroupId, groupId, StringComparison.OrdinalIgnoreCase))
            {
                rule.ObsMediaGroupId = "";
                rule.SendObsMedia = false;
                updated = true;
            }

            if (kind == ObsMediaKind.Image
                && rule.ObsImageSourceMode == MediaSourceMode.Group
                && string.Equals(rule.ObsImageGroupId, groupId, StringComparison.OrdinalIgnoreCase))
            {
                rule.ObsImageGroupId = "";
                rule.SendObsImage = false;
                updated = true;
            }

            if (kind == ObsMediaKind.Video
                && rule.ObsVideoSourceMode == MediaSourceMode.Group
                && string.Equals(rule.ObsVideoGroupId, groupId, StringComparison.OrdinalIgnoreCase))
            {
                rule.ObsVideoGroupId = "";
                rule.SendObsVideo = false;
                updated = true;
            }

            if (updated)
            {
                cleared++;
            }
        }

        return cleared;
    }
}

public readonly record struct LibraryGroupMutation<TGroup>(
    TGroup? Group,
    bool IsValid,
    bool Created)
    where TGroup : class, ILibraryGroupConfig
{
    public static LibraryGroupMutation<TGroup> Invalid() => new(null, false, false);

    public static LibraryGroupMutation<TGroup> Existing(TGroup group) => new(group, true, false);

    public static LibraryGroupMutation<TGroup> NewlyCreated(TGroup group) => new(group, true, true);
}
