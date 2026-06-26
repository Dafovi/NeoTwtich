using NeoTwitch.Models;
using NeoTwitch.Services.Ui;
using NeoTwitch.ViewModels.Library;

namespace NeoTwitch.Services.Library;

public static class LibraryGroupRowFactoryService
{
    public static IReadOnlyList<AudioGroupRow> CreateAudioGroupRows(
        IEnumerable<AudioGroupConfig> groups,
        IEnumerable<AudioAssetConfig> library)
    {
        return groups
            .Select((group, index) =>
            {
                var count = library.Count(audio => string.Equals(audio.GroupId, group.Id, StringComparison.OrdinalIgnoreCase));
                return new AudioGroupRow(
                    group.Id,
                    group.Name,
                    $"{count} audio{(count == 1 ? "" : "s")}",
                    UiBrushFactory.FrozenBrushFrom(AccentForIndex(index)));
            })
            .ToArray();
    }

    public static IReadOnlyList<MediaGroupRow> CreateMediaGroupRows(
        IEnumerable<MediaGroupConfig> groups,
        IEnumerable<MediaAssetConfig> library,
        Func<int, string> countTextFactory)
    {
        return groups
            .Select((group, index) =>
            {
                var count = library.Count(asset => string.Equals(asset.GroupId, group.Id, StringComparison.OrdinalIgnoreCase));
                return new MediaGroupRow(
                    group.Id,
                    group.Name,
                    countTextFactory(count),
                    UiBrushFactory.FrozenBrushFrom(AccentForIndex(index)));
            })
            .ToArray();
    }

    private static string AccentForIndex(int index)
    {
        return (index % 4) switch
        {
            0 => "#14B8A6",
            1 => "#B56CFF",
            2 => "#37C7F3",
            _ => "#22C55E"
        };
    }
}
