using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using NeoTwitch.Shared;

namespace NeoTwitch.Installer;

internal interface IReleaseClient
{
    Task<VerifiedReleaseAsset> DownloadLatestVerifiedAsync(
        string targetDirectory,
        IProgress<InstallProgress> progress,
        CancellationToken cancellationToken);
}

internal sealed class GitHubReleaseClient : IReleaseClient, IDisposable
{
    private const int MaximumManifestBytes = 1024 * 1024;
    private readonly HttpClient _http;
    private readonly bool _ownsHttp;
    private readonly ReleaseIntegrityVerifier _integrityVerifier;

    public GitHubReleaseClient(
        HttpClient? httpClient = null,
        ReleaseIntegrityVerifier? integrityVerifier = null)
    {
        _http = httpClient ?? new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
        _ownsHttp = httpClient is null;
        _integrityVerifier = integrityVerifier ?? ReleaseIntegrityVerifier.CreateProduction();
        _http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(NeoTwitchProduct.GitHubInstallerUserAgent, NeoTwitchProduct.CurrentVersionText));
    }

    public async Task<VerifiedReleaseAsset> DownloadLatestVerifiedAsync(
        string targetDirectory,
        IProgress<InstallProgress> progress,
        CancellationToken cancellationToken)
    {
        GitHubRelease release;
        try
        {
            using var response = await _http.GetAsync(NeoTwitchProduct.LatestReleaseApiUrl, cancellationToken);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            release = await JsonSerializer.DeserializeAsync<GitHubRelease>(stream, cancellationToken: cancellationToken)
                ?? throw new InvalidOperationException("GitHub no devolvió información del release.");
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException or JsonException)
        {
            throw new ReleaseIntegrityException(
                ReleaseIntegrityFailure.DownloadFailure,
                "No se pudo obtener la metadata del release desde GitHub.",
                ex);
        }

        if (string.IsNullOrWhiteSpace(release.TagName) || release.Assets is null)
        {
            throw new ReleaseIntegrityException(
                ReleaseIntegrityFailure.ManifestMalformed,
                "La metadata del release de GitHub está incompleta.");
        }

        var asset = PickBestAsset(release.Assets)
            ?? throw new InvalidOperationException("El último release no tiene un asset instalable de Neo Twitch.");
        if (!string.Equals(asset.Name, Path.GetFileName(asset.Name), StringComparison.Ordinal))
        {
            throw new ReleaseIntegrityException(
                ReleaseIntegrityFailure.ManifestMalformed,
                "GitHub devolvió un nombre de artefacto no válido.");
        }
        var manifestAsset = release.Assets.SingleOrDefault(candidate =>
            string.Equals(candidate.Name, NeoTwitchProduct.ReleaseIntegrityManifestFileName, StringComparison.Ordinal));
        if (manifestAsset is null)
        {
            throw new ReleaseIntegrityException(
                ReleaseIntegrityFailure.ManifestMissing,
                $"El release {release.TagName} no contiene {NeoTwitchProduct.ReleaseIntegrityManifestFileName}.");
        }

        var signatureAsset = release.Assets.SingleOrDefault(candidate =>
            string.Equals(candidate.Name, NeoTwitchProduct.ReleaseIntegritySignatureFileName, StringComparison.Ordinal));
        if (signatureAsset is null)
        {
            throw new ReleaseIntegrityException(
                ReleaseIntegrityFailure.SignatureMissing,
                $"El release {release.TagName} no contiene {NeoTwitchProduct.ReleaseIntegritySignatureFileName}.");
        }

        progress.Report(new InstallProgress(10, $"Verificando manifiesto de {release.TagName}"));
        var manifestBytes = await DownloadSmallAssetAsync(manifestAsset, cancellationToken);
        var signatureBytes = await DownloadSmallAssetAsync(signatureAsset, cancellationToken);
        var trustedManifest = _integrityVerifier.VerifyManifest(
            manifestBytes,
            signatureBytes,
            release.TagName,
            asset.Name);

        progress.Report(new InstallProgress(18, $"Manifiesto firmado válido para {asset.Name}"));
        var releaseAsset = new ReleaseAsset(release.TagName, asset.Name, asset.BrowserDownloadUrl, asset.Size, release.Body ?? "");
        var packagePath = await DownloadAsync(releaseAsset, targetDirectory, progress, cancellationToken);

        progress.Report(new InstallProgress(47, $"Verificando SHA-256 de {asset.Name}"));
        await _integrityVerifier.VerifyArtifactAsync(packagePath, trustedManifest.Artifact, cancellationToken);
        progress.Report(new InstallProgress(49, $"Artefacto verificado: {asset.Name}"));

        return new VerifiedReleaseAsset(
            trustedManifest.Version,
            asset.Name,
            packagePath,
            release.Body ?? "");
    }

    private async Task<string> DownloadAsync(
        ReleaseAsset asset,
        string targetDirectory,
        IProgress<InstallProgress> progress,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(targetDirectory);
        var targetPath = Path.Combine(targetDirectory, asset.Name);

        try
        {
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
                    var percent = 20 + (int)Math.Round(readTotal * 25d / totalBytes);
                    progress.Report(new InstallProgress(percent, $"Descargando {asset.Name}"));
                }
            }

            return targetPath;
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException)
        {
            throw new ReleaseIntegrityException(
                ReleaseIntegrityFailure.DownloadFailure,
                $"No se pudo descargar '{asset.Name}' desde GitHub.",
                ex);
        }
    }

    private async Task<byte[]> DownloadSmallAssetAsync(GitHubAsset asset, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _http.GetAsync(asset.BrowserDownloadUrl, cancellationToken);
            response.EnsureSuccessStatusCode();
            var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
            if (bytes.Length == 0 || bytes.Length > MaximumManifestBytes)
            {
                throw new InvalidDataException($"'{asset.Name}' tiene un tamaño inválido.");
            }

            return bytes;
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException)
        {
            throw new ReleaseIntegrityException(
                ReleaseIntegrityFailure.DownloadFailure,
                $"No se pudo descargar '{asset.Name}' desde GitHub.",
                ex);
        }
    }

    private static GitHubAsset? PickBestAsset(IReadOnlyList<GitHubAsset> assets)
    {
        return assets
            .Where(asset => asset.Name.Contains(Path.GetFileNameWithoutExtension(NeoTwitchProduct.AppExecutableName), StringComparison.OrdinalIgnoreCase))
            .Where(asset => !asset.Name.Contains("Installer", StringComparison.OrdinalIgnoreCase))
            .Where(asset => AssetRank(asset.Name) < 100)
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

        if (name.Equals(NeoTwitchProduct.AppExecutableName, StringComparison.OrdinalIgnoreCase))
        {
            return 3;
        }

        if (name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
        {
            return 4;
        }

        return 100;
    }

    public void Dispose()
    {
        if (_ownsHttp)
        {
            _http.Dispose();
        }
    }

    private sealed record GitHubRelease(
        [property: JsonPropertyName("tag_name")] string? TagName,
        [property: JsonPropertyName("body")] string? Body,
        [property: JsonPropertyName("assets")] IReadOnlyList<GitHubAsset>? Assets);

    private sealed record GitHubAsset(
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("browser_download_url")] string BrowserDownloadUrl,
        [property: JsonPropertyName("size")] long Size);
}

internal sealed record ReleaseAsset(string Version, string Name, string DownloadUrl, long Size, string ReleaseNotes);

internal sealed record VerifiedReleaseAsset(string Version, string Name, string PackagePath, string ReleaseNotes);
