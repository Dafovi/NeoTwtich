using NeoTwitch.Models;
using NeoTwitch.Services.Ui;
using NeoTwitch.ViewModels.Library;
using static NeoTwitch.Services.Ui.UiBrushFactory;

namespace NeoTwitch.Services.Library;

public static class LibraryRowFactoryService
{
    public static AudioLibraryRow CreateAudioRow(
        AudioAssetConfig audio,
        IEnumerable<EventRule> rules,
        IReadOnlyDictionary<string, string> groupsById,
        string noGroupText,
        string? previewingAudioId,
        bool isAudioPreviewActive,
        int index)
    {
        var assignedRules = rules
            .Where(rule => AudioRuleAssetService.RuleUsesAudioAsset(rule, audio))
            .ToArray();
        var assignedText = assignedRules.Length switch
        {
            0 => "",
            1 => assignedRules[0].Name,
            _ => $"{assignedRules[0].Name} +{assignedRules.Length - 1}"
        };
        var accentColor = assignedRules.Length > 0
            ? UiAccentCatalog.ForEventKind(assignedRules[0].EventKind)
            : "#64748B";

        return new AudioLibraryRow(
            audio.Id,
            audio.DisplayName,
            audio.FilePath,
            audio.GroupId,
            assignedText,
            groupsById.TryGetValue(audio.GroupId, out var groupName) ? groupName : noGroupText,
            audio.DurationText,
            assignedRules.Length > 0,
            string.Equals(previewingAudioId, audio.Id, StringComparison.OrdinalIgnoreCase) && isAudioPreviewActive,
            FrozenBrushFrom(accentColor),
            TranslucentBrushFrom(accentColor),
            index);
    }

    public static MediaLibraryRow CreateMediaRow(
        MediaLibraryKind kind,
        MediaAssetConfig asset,
        IReadOnlyDictionary<string, string> groupsById,
        string noGroupText,
        int index,
        bool canPreview,
        MediaLibraryKind? previewingMediaKind,
        string? previewingMediaId)
    {
        var info = MediaLibraryKindCatalog.Get(kind);
        var metadata = kind == MediaLibraryKind.Image
            ? asset.ResolutionText
            : MediaMetadataService.BuildVideoMetadata(asset);

        return new MediaLibraryRow(
            asset.Id,
            asset.DisplayName,
            asset.FilePath,
            asset.GroupId,
            groupsById.TryGetValue(asset.GroupId, out var groupName) ? groupName : noGroupText,
            metadata,
            info.IconPath,
            FrozenBrushFrom(info.AccentColor),
            TranslucentBrushFrom(info.AccentColor),
            index,
            canPreview,
            previewingMediaKind == kind && string.Equals(previewingMediaId, asset.Id, StringComparison.OrdinalIgnoreCase));
    }
}
