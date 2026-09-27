using System.Net.Http.Headers;
using System.Net.Http;
using System.Text.Json;
using NeoTwitch.Models;

namespace NeoTwitch.Services.YouTube;

/// <summary>
/// Reads the user's active YouTube broadcast. Chat ingestion is deliberately a separate service
/// so a stream status request cannot make alert processing block on a chat transport.
/// </summary>
public sealed class YouTubeLiveService : IDisposable
{
    private const string ActiveBroadcastsUrl = "https://www.googleapis.com/youtube/v3/liveBroadcasts?part=snippet,status&mine=true&broadcastType=all";
    private readonly HttpClient _http;
    private readonly bool _ownsHttpClient;
    private int _disposed;

    public YouTubeLiveService(HttpClient? httpClient = null)
    {
        _http = httpClient ?? new HttpClient();
        _ownsHttpClient = httpClient is null;
    }

    public async Task<YouTubeLiveBroadcastStatus> GetActiveBroadcastAsync(
        YouTubeTokenInfo token,
        CancellationToken cancellationToken)
    {
        if (!token.HasToken)
        {
            return YouTubeLiveBroadcastStatus.Offline;
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, ActiveBroadcastsUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
        using var response = await _http.SendAsync(request, cancellationToken);
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"No fue posible consultar el directo de YouTube (HTTP {(int)response.StatusCode}; {DescribeApiError(json)})." );
        }

        using var document = JsonDocument.Parse(json);
        if (!document.RootElement.TryGetProperty("items", out var items)
            || items.GetArrayLength() == 0)
        {
            return YouTubeLiveBroadcastStatus.Offline;
        }

        var broadcast = items.EnumerateArray().FirstOrDefault(IsActiveBroadcast);
        if (broadcast.ValueKind == JsonValueKind.Undefined)
        {
            return YouTubeLiveBroadcastStatus.Offline;
        }

        var snippet = broadcast.TryGetProperty("snippet", out var value) ? value : default;
        return new YouTubeLiveBroadcastStatus(
            true,
            broadcast.TryGetProperty("id", out var id) ? id.GetString() ?? "" : "",
            snippet.ValueKind != JsonValueKind.Undefined && snippet.TryGetProperty("title", out var title)
                ? title.GetString() ?? ""
                : "",
            snippet.ValueKind != JsonValueKind.Undefined && snippet.TryGetProperty("liveChatId", out var chatId)
                ? chatId.GetString() ?? ""
                : "");
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 0 && _ownsHttpClient)
        {
            _http.Dispose();
        }
    }

    private static string DescribeApiError(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            var error = document.RootElement.TryGetProperty("error", out var errorValue) ? errorValue : default;
            var message = error.ValueKind == JsonValueKind.Object
                && error.TryGetProperty("message", out var messageValue)
                ? messageValue.GetString()
                : null;
            var safeMessage = new string((message ?? "detalle remoto no disponible")
                .Where(character => char.IsLetterOrDigit(character)
                    || character is ' ' or '-' or '_' or '.' or ':' or '/')
                .Take(180)
                .ToArray())
                .Trim();
            return string.IsNullOrWhiteSpace(safeMessage) ? "detalle remoto no disponible" : safeMessage;
        }
        catch
        {
            return "detalle remoto no disponible";
        }
    }

    private static bool IsActiveBroadcast(JsonElement broadcast)
    {
        if (!broadcast.TryGetProperty("status", out var status)
            || !status.TryGetProperty("lifeCycleStatus", out var lifeCycleStatus))
        {
            return false;
        }

        var value = lifeCycleStatus.GetString();
        return string.Equals(value, "live", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "testing", StringComparison.OrdinalIgnoreCase);
    }
}

public sealed record YouTubeLiveBroadcastStatus(
    bool IsLive,
    string BroadcastId,
    string Title,
    string LiveChatId)
{
    public static YouTubeLiveBroadcastStatus Offline { get; } = new(false, "", "", "");
}
