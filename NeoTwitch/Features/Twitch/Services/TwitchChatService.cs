using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using NeoTwitch.Models;
using NeoTwitch.Services.Text;

namespace NeoTwitch.Services;

public sealed class TwitchChatService : IDisposable
{
    private readonly HttpClient _http;
    private readonly bool _ownsHttpClient;
    private readonly IUiTextService _text;

    public TwitchChatService(IUiTextService text, HttpClient? httpClient = null)
    {
        _text = text;
        _http = httpClient ?? new HttpClient();
        _ownsHttpClient = httpClient is null;
    }

    public async Task SendMessageAsync(AppConfig config, string message, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        if (!config.Channel.IsReady)
        {
            throw new InvalidOperationException(_text.Get(UiTextKeys.TwitchChatMissingChannel));
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, TwitchEventSubProtocol.ChatMessagesApiUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", config.Token.AccessToken);
        request.Headers.Add("Client-Id", config.TwitchClientId);
        request.Content = new StringContent(
            JsonSerializer.Serialize(new
            {
                broadcaster_id = config.Channel.UserId,
                sender_id = config.Channel.UserId,
                message
            }),
            Encoding.UTF8,
            TwitchEventSubProtocol.ContentTypeJson);

        using var response = await _http.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                _text.Format(UiTextKeys.TwitchChatSendFailure, $"HTTP {(int)response.StatusCode}"));
        }
    }

    public string FormatMessage(string template, TwitchEvent twitchEvent)
    {
        if (string.IsNullOrWhiteSpace(template))
        {
            return "";
        }

        var userName = string.IsNullOrWhiteSpace(twitchEvent.UserName)
            ? _text.Get(UiTextKeys.TwitchChatAnonymousUser)
            : twitchEvent.UserName;
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["user"] = userName,
            ["bits"] = (twitchEvent.Bits ?? 0).ToString(),
            ["reward"] = twitchEvent.RewardTitle ?? "",
            ["viewers"] = (twitchEvent.ViewerCount ?? 0).ToString(),
            ["message"] = twitchEvent.Message ?? "",
            ["event"] = DisplayNameService.For(twitchEvent.Kind, _text)
        };

        var result = template;
        foreach (var item in values)
        {
            result = result.Replace($"{{{item.Key}}}", item.Value, StringComparison.OrdinalIgnoreCase);
        }

        return result.Trim();
    }

    public void Dispose()
    {
        if (_ownsHttpClient)
        {
            _http.Dispose();
        }
    }
}
