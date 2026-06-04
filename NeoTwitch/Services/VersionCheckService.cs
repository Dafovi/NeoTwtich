using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NeoTwitch.Services;

public sealed class VersionCheckService
{
    public const string ReleasesUrl = "https://github.com/Dafovi/NeoTwtich/releases";
    public const string LatestReleaseUrl = "https://github.com/Dafovi/NeoTwtich/releases/latest";

    private const string LatestReleaseApiUrl = "https://api.github.com/repos/Dafovi/NeoTwtich/releases/latest";

    private readonly HttpClient _http = new()
    {
        Timeout = TimeSpan.FromSeconds(8)
    };

    public VersionCheckService()
    {
        _http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("NeoTwitch", CurrentVersionText));
    }

    public static string CurrentVersionText => NormalizeVersionText(
        Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion
        ?? Assembly.GetExecutingAssembly().GetName().Version?.ToString()
        ?? "0.0.0");

    public async Task<VersionCheckResult> CheckLatestAsync(CancellationToken cancellationToken)
    {
        using var response = await _http.GetAsync(LatestReleaseApiUrl, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var release = await JsonSerializer.DeserializeAsync<GitHubRelease>(stream, cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("GitHub no devolvio informacion de release.");

        var latestVersionText = NormalizeVersionText(release.TagName);
        var currentVersionText = CurrentVersionText;
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
        var normalized = NormalizeVersionText(value);
        return Version.TryParse(normalized, out version!);
    }

    private static string NormalizeVersionText(string? value)
    {
        var text = (value ?? "0.0.0").Trim();
        if (text.StartsWith('v') || text.StartsWith('V'))
        {
            text = text[1..];
        }

        var metadataIndex = text.IndexOfAny(['-', '+']);
        if (metadataIndex >= 0)
        {
            text = text[..metadataIndex];
        }

        return string.IsNullOrWhiteSpace(text) ? "0.0.0" : text;
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
