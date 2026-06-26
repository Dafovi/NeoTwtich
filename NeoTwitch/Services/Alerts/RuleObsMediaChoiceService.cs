using NeoTwitch.Models;

namespace NeoTwitch.Services.Alerts;

public static class RuleObsMediaChoiceService
{
    public static RuleObsMediaChoices Resolve(
        ObsMediaKind kind,
        IReadOnlyList<MediaAssetConfig> imageLibrary,
        IReadOnlyList<MediaAssetConfig> videoLibrary,
        IReadOnlyList<MediaGroupConfig> imageGroups,
        IReadOnlyList<MediaGroupConfig> videoGroups)
    {
        return kind == ObsMediaKind.Video
            ? new RuleObsMediaChoices(videoLibrary, videoGroups)
            : new RuleObsMediaChoices(imageLibrary, imageGroups);
    }
}

public sealed record RuleObsMediaChoices(
    IReadOnlyList<MediaAssetConfig> Assets,
    IReadOnlyList<MediaGroupConfig> Groups)
{
    public bool HasAssets => Assets.Count > 0;
    public bool HasGroups => Groups.Count > 0;
}
