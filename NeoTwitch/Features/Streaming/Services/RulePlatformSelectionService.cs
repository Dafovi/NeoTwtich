using System.Collections.ObjectModel;
using NeoTwitch.Models;

namespace NeoTwitch.Services.Streaming;

public static class RulePlatformSelectionService
{
    public static ObservableCollection<StreamingPlatform> Normalize(IEnumerable<StreamingPlatform>? platforms)
    {
        var normalized = (platforms ?? [])
            .Where(Enum.IsDefined)
            .Distinct()
            .ToArray();

        return normalized.Length > 0
            ? new ObservableCollection<StreamingPlatform>(normalized)
            : new ObservableCollection<StreamingPlatform>([StreamingPlatform.Twitch]);
    }

    public static bool Includes(EventRule rule, StreamingPlatform platform) =>
        rule.SourcePlatforms.Contains(platform);
}
