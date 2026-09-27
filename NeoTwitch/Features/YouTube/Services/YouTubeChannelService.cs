using System.Net.Http.Headers;
using System.Net.Http;
using System.Text.Json;
using NeoTwitch.Models;

namespace NeoTwitch.Services.YouTube;

public sealed class YouTubeChannelService : IDisposable
{
    private const string CurrentChannelUrl = "https://www.googleapis.com/youtube/v3/channels?part=snippet&mine=true";
    private readonly HttpClient _http;
    private readonly bool _ownsHttpClient;

    public YouTubeChannelService(HttpClient? httpClient = null)
    {
        _http = httpClient ?? new HttpClient();
        _ownsHttpClient = httpClient is null;
    }

    public async Task<YouTubeChannelInfo> GetCurrentChannelAsync(
        YouTubeTokenInfo token,
        CancellationToken cancellationToken)
    {
        if (!token.HasToken)
        {
            throw new InvalidOperationException("Conecta una cuenta de YouTube antes de consultar el canal.");
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, CurrentChannelUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
        using var response = await _http.SendAsync(request, cancellationToken);
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"No fue posible consultar el canal de YouTube (HTTP {(int)response.StatusCode}).");
        }

        using var document = JsonDocument.Parse(json);
        if (!document.RootElement.TryGetProperty("items", out var items) || items.GetArrayLength() == 0)
        {
            throw new InvalidOperationException("YouTube no devolvió ningún canal para la cuenta autorizada.");
        }

        var channel = items[0];
        var snippet = channel.TryGetProperty("snippet", out var snippetValue) ? snippetValue : default;
        var thumbnails = snippet.ValueKind == JsonValueKind.Object && snippet.TryGetProperty("thumbnails", out var thumbnailsValue)
            ? thumbnailsValue
            : default;
        var thumbnail = ReadThumbnail(thumbnails, "medium")
            ?? ReadThumbnail(thumbnails, "default")
            ?? "";

        return new YouTubeChannelInfo
        {
            ChannelId = channel.TryGetProperty("id", out var id) ? id.GetString() ?? "" : "",
            DisplayName = snippet.ValueKind == JsonValueKind.Object && snippet.TryGetProperty("title", out var title)
                ? title.GetString() ?? ""
                : "",
            ThumbnailUrl = thumbnail
        };
    }

    public void Dispose()
    {
        if (_ownsHttpClient)
        {
            _http.Dispose();
        }
    }

    private static string? ReadThumbnail(JsonElement thumbnails, string size)
    {
        return thumbnails.ValueKind == JsonValueKind.Object
            && thumbnails.TryGetProperty(size, out var image)
            && image.TryGetProperty("url", out var url)
            ? url.GetString()
            : null;
    }
}
