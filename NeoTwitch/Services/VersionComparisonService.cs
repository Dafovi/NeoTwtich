using NeoTwitch.Shared;

namespace NeoTwitch.Services;

public static class VersionComparisonService
{
    public static bool IsNewer(string latestVersionText, string currentVersionText)
    {
        return TryParseVersion(latestVersionText, out var latestVersion)
            && TryParseVersion(currentVersionText, out var currentVersion)
            && latestVersion.CompareTo(currentVersion) > 0;
    }

    public static bool TryParseVersion(string value, out Version version)
    {
        var normalized = NeoTwitchProduct.NormalizeVersionText(value);
        return Version.TryParse(normalized, out version!);
    }
}
