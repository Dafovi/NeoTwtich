using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using NeoTwitch.Shared;

namespace NeoTwitch.Services;

public sealed class VersionCheckService
{
    public static string ReleasesUrl => NeoTwitchProduct.ReleasesUrl;
    public static string LatestReleaseUrl => NeoTwitchProduct.LatestReleaseUrl;

    private readonly HttpClient _http = new()
    {
        Timeout = TimeSpan.FromSeconds(8)
    };

    public VersionCheckService()
    {
        _http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(NeoTwitchProduct.GitHubAppUserAgent, NeoTwitchProduct.CurrentVersionText));
    }

    public async Task<VersionCheckResult> CheckLatestAsync(CancellationToken cancellationToken)
    {
        using var response = await _http.GetAsync(NeoTwitchProduct.LatestReleaseApiUrl, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var release = await JsonSerializer.DeserializeAsync<GitHubRelease>(stream, cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("GitHub no devolvio informacion de release.");

        var latestVersionText = NeoTwitchProduct.NormalizeVersionText(release.TagName);
        var currentVersionText = NeoTwitchProduct.CurrentVersionText;
        var isNewer = IsNewer(latestVersionText, currentVersionText);
        var releaseUrl = string.IsNullOrWhiteSpace(release.HtmlUrl)
            ? LatestReleaseUrl
            : release.HtmlUrl;

        return new VersionCheckResult(currentVersionText, latestVersionText, releaseUrl, isNewer);
    }

    private static bool IsNewer(string latestVersionText, string currentVersionText)
    {
        return TryParseVersion(latestVersionText, out var latestVersion)
            && TryParseVersion(currentVersionText, out var currentVersion)
            && latestVersion.CompareTo(currentVersion) > 0;
    }

    private static bool TryParseVersion(string value, out Version version)
    {
        var normalized = NeoTwitchProduct.NormalizeVersionText(value);
        return Version.TryParse(normalized, out version!);
    }

    private sealed record GitHubRelease(
        [property: JsonPropertyName("tag_name")] string TagName,
        [property: JsonPropertyName("html_url")] string HtmlUrl);
}

public sealed record VersionCheckResult(
    string CurrentVersion,
    string LatestVersion,
    string ReleaseUrl,
    bool IsUpdateAvailable);
