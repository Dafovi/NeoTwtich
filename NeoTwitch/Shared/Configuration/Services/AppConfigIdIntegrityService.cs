using NeoTwitch.Models;
using NeoTwitch.Models.Library;

namespace NeoTwitch.Services.Configuration;

public sealed record AppConfigIntegrityReport(
    IReadOnlyList<string> RepairedIds,
    IReadOnlyList<string> RepairedReferences,
    IReadOnlyList<string> AmbiguousReferences)
{
    public bool HasWarnings => AmbiguousReferences.Count > 0;
}

public static class AppConfigIdIntegrityService
{
    public static AppConfigIntegrityReport Repair(AppConfig config, Func<string> idFactory)
    {
        var repairedIds = new List<string>();
        var repairedReferences = new List<string>();
        var ambiguousReferences = new List<string>();

        _ = RepairDomain(config.Rules, rule => rule.Id, (rule, id) => rule.Id = id, "regla", idFactory, repairedIds);
        var audioGroups = RepairDomain(config.AudioGroups, group => group.Id, (group, id) => group.Id = id, "grupo de audio", idFactory, repairedIds);
        var audioAssets = RepairDomain(config.AudioLibrary, asset => asset.Id, (asset, id) => asset.Id = id, "audio", idFactory, repairedIds);
        var imageGroups = RepairDomain(config.ImageGroups, group => group.Id, (group, id) => group.Id = id, "grupo de imagen", idFactory, repairedIds);
        var imageAssets = RepairDomain(config.ImageLibrary, asset => asset.Id, (asset, id) => asset.Id = id, "imagen", idFactory, repairedIds);
        var videoGroups = RepairDomain(config.VideoGroups, group => group.Id, (group, id) => group.Id = id, "grupo de video", idFactory, repairedIds);
        var videoAssets = RepairDomain(config.VideoLibrary, asset => asset.Id, (asset, id) => asset.Id = id, "video", idFactory, repairedIds);
        _ = RepairDomain(config.LedStrips, strip => strip.Id, (strip, id) => strip.Id = id, "tira LED", idFactory, repairedIds);

        RepairAssetGroupReferences(config.AudioLibrary, audioGroups, "audio.groupId", repairedReferences, ambiguousReferences);
        RepairAssetGroupReferences(config.ImageLibrary, imageGroups, "image.groupId", repairedReferences, ambiguousReferences);
        RepairAssetGroupReferences(config.VideoLibrary, videoGroups, "video.groupId", repairedReferences, ambiguousReferences);

        foreach (var rule in config.Rules)
        {
            rule.AudioAssetId = RepairReference(rule.AudioAssetId, audioAssets, $"regla {rule.Id}.audioAssetId", repairedReferences, ambiguousReferences);
            rule.AudioGroupId = RepairReference(rule.AudioGroupId, audioGroups, $"regla {rule.Id}.audioGroupId", repairedReferences, ambiguousReferences);
            rule.ObsImageAssetId = RepairReference(rule.ObsImageAssetId, imageAssets, $"regla {rule.Id}.obsImageAssetId", repairedReferences, ambiguousReferences);
            rule.ObsImageGroupId = RepairReference(rule.ObsImageGroupId, imageGroups, $"regla {rule.Id}.obsImageGroupId", repairedReferences, ambiguousReferences);
            rule.ObsVideoAssetId = RepairReference(rule.ObsVideoAssetId, videoAssets, $"regla {rule.Id}.obsVideoAssetId", repairedReferences, ambiguousReferences);
            rule.ObsVideoGroupId = RepairReference(rule.ObsVideoGroupId, videoGroups, $"regla {rule.Id}.obsVideoGroupId", repairedReferences, ambiguousReferences);
        }

        return new AppConfigIntegrityReport(repairedIds, repairedReferences, ambiguousReferences);
    }

    private static IdentityDomain RepairDomain<T>(
        IEnumerable<T> items,
        Func<T, string> getId,
        Action<T, string> setId,
        string label,
        Func<string> idFactory,
        ICollection<string> repairedIds)
    {
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var duplicateOriginalIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in items)
        {
            var original = getId(item) ?? "";
            if (!string.IsNullOrWhiteSpace(original) && used.Add(original))
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(original))
            {
                duplicateOriginalIds.Add(original);
            }

            var replacement = CreateUniqueId(used, idFactory);
            setId(item, replacement);
            repairedIds.Add($"{label}:{(string.IsNullOrWhiteSpace(original) ? "<vacío>" : original)}->{replacement}");
        }

        return new IdentityDomain(used, duplicateOriginalIds);
    }

    private static string CreateUniqueId(ISet<string> used, Func<string> idFactory)
    {
        for (var attempt = 0; attempt < 1024; attempt++)
        {
            var candidate = idFactory()?.Trim() ?? "";
            if (!string.IsNullOrWhiteSpace(candidate) && used.Add(candidate))
            {
                return candidate;
            }
        }

        throw new InvalidOperationException("No se pudo generar un identificador de configuración único.");
    }

    private static void RepairAssetGroupReferences<TAsset>(
        IEnumerable<TAsset> assets,
        IdentityDomain groups,
        string label,
        ICollection<string> repairedReferences,
        ICollection<string> ambiguousReferences)
        where TAsset : ILibraryAssetConfig
    {
        foreach (var asset in assets)
        {
            asset.GroupId = RepairReference(
                asset.GroupId,
                groups,
                $"{label} de {asset.Id}",
                repairedReferences,
                ambiguousReferences);
        }
    }

    private static string RepairReference(
        string? reference,
        IdentityDomain domain,
        string label,
        ICollection<string> repairedReferences,
        ICollection<string> ambiguousReferences)
    {
        if (string.IsNullOrWhiteSpace(reference))
        {
            return "";
        }

        if (domain.DuplicateOriginalIds.Contains(reference))
        {
            ambiguousReferences.Add($"{label}:{reference}");
            return "";
        }

        if (domain.ValidIds.Contains(reference))
        {
            return reference;
        }

        repairedReferences.Add($"{label}:{reference}-><vacío>");
        return "";
    }

    private sealed record IdentityDomain(
        HashSet<string> ValidIds,
        HashSet<string> DuplicateOriginalIds);
}

public sealed record AppConfigNormalizationResult(
    AppConfig Config,
    AppConfigIntegrityReport IntegrityReport);
