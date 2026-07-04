using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using NeoTwitch.Services.Text;
using NeoTwitch.Shared;

namespace NeoTwitch.Services;

public sealed class VersionCheckService
{
    private readonly IUiTextService _text;
    private readonly HttpClient _http;

    public VersionCheckService(IUiTextService text, HttpClient? httpClient = null)
    {
        _text = text;
        _http = httpClient ?? new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(8)
        };
        _http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(NeoTwitchProduct.GitHubAppUserAgent, NeoTwitchProduct.CurrentVersionText));
    }

    public async Task<VersionCheckResult> CheckLatestAsync(CancellationToken cancellationToken)
    {
        using var response = await _http.GetAsync(NeoTwitchProduct.LatestReleaseApiUrl, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var release = await JsonSerializer.DeserializeAsync<GitHubRelease>(stream, cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException(_text.Get(UiTextKeys.VersionCheckEmptyReleaseResponse));

        var latestVersionText = NeoTwitchProduct.NormalizeVersionText(release.TagName);
        var currentVersionText = NeoTwitchProduct.CurrentVersionText;
        var isNewer = VersionComparisonService.IsNewer(latestVersionText, currentVersionText);
        var releaseUrl = string.IsNullOrWhiteSpace(release.HtmlUrl)
            ? NeoTwitchProduct.LatestReleaseUrl
            : release.HtmlUrl;

        return new VersionCheckResult(currentVersionText, latestVersionText, releaseUrl, isNewer);
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
