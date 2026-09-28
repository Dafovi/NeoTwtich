using System.Net.Http.Headers;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using NeoTwitch.Models;

namespace NeoTwitch.Services.YouTube;

public sealed class YouTubeLiveChatService : IDisposable
{
    private const string MessagesUrl = "https://www.googleapis.com/youtube/v3/liveChat/messages?part=snippet,authorDetails";
    private readonly HttpClient _http;
    private readonly bool _ownsHttpClient;

    public YouTubeLiveChatService(HttpClient? httpClient = null)
    {
        _http = httpClient ?? new HttpClient();
        _ownsHttpClient = httpClient is null;
    }

    public async Task<YouTubeLiveChatPage> GetMessagesAsync(
        YouTubeTokenInfo token,
        string liveChatId,
        string? pageToken,
        CancellationToken cancellationToken)
    {
        if (!token.HasToken)
        {
            throw new InvalidOperationException("La cuenta de YouTube necesita autorizarse de nuevo.");
        }

        if (string.IsNullOrWhiteSpace(liveChatId))
        {
            throw new ArgumentException("Falta el identificador del chat en vivo de YouTube.", nameof(liveChatId));
        }

        var uri = BuildMessagesUri(liveChatId, pageToken);
        using var request = new HttpRequestMessage(HttpMethod.Get, uri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
        using var response = await _http.SendAsync(request, cancellationToken);
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"No fue posible leer el chat de YouTube (HTTP {(int)response.StatusCode}).");
        }

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        var messages = root.TryGetProperty("items", out var items)
            ? items.EnumerateArray().Select(ParseMessage).Where(message => message is not null).Cast<YouTubeLiveChatMessage>().ToArray()
            : [];
        var nextPageToken = root.TryGetProperty("nextPageToken", out var nextToken) ? nextToken.GetString() ?? "" : "";
        var pollingMilliseconds = root.TryGetProperty("pollingIntervalMillis", out var interval) && interval.TryGetInt32(out var value)
            ? value
            : 5_000;

        return new YouTubeLiveChatPage(
            messages,
            nextPageToken,
            TimeSpan.FromMilliseconds(Math.Clamp(pollingMilliseconds, 1_000, 30_000)),
            root.TryGetProperty("offlineAt", out _));
    }

    public async Task SendMessageAsync(
        YouTubeTokenInfo token,
        string liveChatId,
        string message,
        CancellationToken cancellationToken)
    {
        if (!token.HasToken)
        {
            throw new InvalidOperationException("La cuenta de YouTube necesita autorizarse de nuevo.");
        }

        if (string.IsNullOrWhiteSpace(liveChatId))
        {
            throw new ArgumentException("Falta el identificador del chat en vivo de YouTube.", nameof(liveChatId));
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, MessagesUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
        request.Content = JsonContent.Create(new
        {
            snippet = new
            {
                liveChatId,
                type = "textMessageEvent",
                textMessageDetails = new { messageText = message.Trim() }
            }
        });
        using var response = await _http.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"No fue posible enviar el mensaje al chat de YouTube (HTTP {(int)response.StatusCode}).");
        }
    }

    public void Dispose()
    {
        if (_ownsHttpClient)
        {
            _http.Dispose();
        }
    }

    private static Uri BuildMessagesUri(string liveChatId, string? pageToken)
    {
        var query = new Dictionary<string, string>
        {
            ["part"] = "snippet,authorDetails",
            ["liveChatId"] = liveChatId
        };
        if (!string.IsNullOrWhiteSpace(pageToken))
        {
            query["pageToken"] = pageToken;
        }

        return new UriBuilder(MessagesUrl)
        {
            Query = string.Join('&', query.Select(pair =>
                $"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(pair.Value)}"))
        }.Uri;
    }

    private static YouTubeLiveChatMessage? ParseMessage(JsonElement item)
    {
        if (!item.TryGetProperty("id", out var id)
            || !item.TryGetProperty("snippet", out var snippet))
        {
            return null;
        }

        var type = ReadString(snippet, "type");
        var publishedAt = DateTimeOffset.TryParse(ReadString(snippet, "publishedAt"), out var published)
            ? published
            : DateTimeOffset.UtcNow;
        var author = item.TryGetProperty("authorDetails", out var authorDetails) ? authorDetails : default;
        var superChat = snippet.TryGetProperty("superChatDetails", out var superChatDetails) ? superChatDetails : default;
        var superSticker = snippet.TryGetProperty("superStickerDetails", out var superStickerDetails) ? superStickerDetails : default;
        var contribution = superChat.ValueKind == JsonValueKind.Object ? superChat : superSticker;
        var membership = snippet.TryGetProperty("newSponsorDetails", out var sponsorDetails) ? sponsorDetails : default;
        var textDetails = snippet.TryGetProperty("textMessageDetails", out var text) ? text : default;

        return new YouTubeLiveChatMessage(
            id.GetString() ?? "",
            type,
            publishedAt,
            ReadString(author, "displayName"),
            ReadString(textDetails, "messageText", ReadString(snippet, "displayMessage")),
            ReadInt(contribution, "tier"),
            ReadString(contribution, "amountDisplayString"),
            ReadString(contribution, "currency"),
            ReadLong(contribution, "amountMicros"),
            ReadString(membership, "memberLevelName"));
    }

    private static string ReadString(JsonElement element, string name, string fallback = "") =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out var value)
            ? value.GetString() ?? fallback
            : fallback;

    private static int? ReadInt(JsonElement element, string name) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out var value) && value.TryGetInt32(out var parsed)
            ? parsed
            : null;

    private static long? ReadLong(JsonElement element, string name) =>
        element.ValueKind == JsonValueKind.Object
        && element.TryGetProperty(name, out var value)
        && (value.ValueKind == JsonValueKind.Number
            ? value.TryGetInt64(out var numericValue)
            : long.TryParse(value.GetString(), out numericValue))
                ? numericValue
                : null;
}
