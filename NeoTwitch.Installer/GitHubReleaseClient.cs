using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NeoTwitch.Installer;

internal sealed class GitHubReleaseClient
{
    private const string LatestReleaseApiUrl = "https://api.github.com/repos/Dafovi/NeoTwtich/releases/latest";

    private readonly HttpClient _http = new()
    {
        Timeout = TimeSpan.FromSeconds(30)
    };

    public GitHubReleaseClient()
    {
        _http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("NeoTwitchInstaller", "2.1.1"));
    }

    public async Task<ReleaseAsset> GetLatestInstallAssetAsync(CancellationToken cancellationToken)
    {
        using var response = await _http.GetAsync(LatestReleaseApiUrl, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var release = await JsonSerializer.DeserializeAsync<GitHubRelease>(stream, cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("GitHub no devolvio informacion del release.");

        var asset = PickBestAsset(release.Assets)
            ?? throw new InvalidOperationException("El ultimo release no tiene un asset instalable de Neo Twitch.");

        return new ReleaseAsset(release.TagName, asset.Name, asset.BrowserDownloadUrl, asset.Size);
    }

    public async Task<string> DownloadAsync(
        ReleaseAsset asset,
        string targetDirectory,
        IProgress<InstallProgress> progress,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(targetDirectory);
        var targetPath = Path.Combine(targetDirectory, asset.Name);

        using var response = await _http.GetAsync(asset.DownloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength ?? asset.Size;
        await using var input = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using var output = File.Create(targetPath);

        var buffer = new byte[81920];
        long readTotal = 0;
        int read;
        while ((read = await input.ReadAsync(buffer, cancellationToken)) > 0)
        {
            await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
            readTotal += read;

            if (totalBytes > 0)
            {
                var percent = 10 + (int)Math.Round(readTotal * 35d / totalBytes);
                progress.Report(new InstallProgress(percent, $"Descargando {asset.Name}"));
            }
        }

        return targetPath;
    }

    private static GitHubAsset? PickBestAsset(IReadOnlyList<GitHubAsset> assets)
    {
        return assets
            .Where(asset => asset.Name.Contains("NeoTwitch", StringComparison.OrdinalIgnoreCase))
            .Where(asset => !asset.Name.Contains("Installer", StringComparison.OrdinalIgnoreCase))
            .OrderBy(asset => AssetRank(asset.Name))
            .FirstOrDefault();
    }

    private static int AssetRank(string name)
    {
        if (name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)
            && name.Contains("Full", StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }

        if (name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)
            && name.Contains("Windows", StringComparison.OrdinalIgnoreCase))
        {
            return 1;
        }

        if (name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
        {
            return 2;
        }

        if (name.Equals("NeoTwitch.exe", StringComparison.OrdinalIgnoreCase))
        {
            return 3;
        }

        if (name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
        {
            return 4;
        }

        return 100;
    }

    private sealed record GitHubRelease(
        [property: JsonPropertyName("tag_name")] string TagName,
        [property: JsonPropertyName("assets")] IReadOnlyList<GitHubAsset> Assets);

    private sealed record GitHubAsset(
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("browser_download_url")] string BrowserDownloadUrl,
        [property: JsonPropertyName("size")] long Size);
}

internal sealed record ReleaseAsset(string Version, string Name, string DownloadUrl, long Size);
