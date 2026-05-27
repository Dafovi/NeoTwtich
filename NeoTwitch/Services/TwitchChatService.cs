using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using NeoTwitch.Models;

namespace NeoTwitch.Services;

public sealed class TwitchChatService : IDisposable
{
    private readonly HttpClient _http = new();

    public async Task SendMessageAsync(AppConfig config, string message, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        if (!config.Channel.IsReady)
        {
            throw new InvalidOperationException("Twitch no tiene canal configurado.");
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.twitch.tv/helix/chat/messages");
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
            "application/json");

        using var response = await _http.SendAsync(request, cancellationToken);
        var responseText = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Twitch no envio el mensaje al chat: {responseText}");
        }
    }

    public static string FormatMessage(string template, TwitchEvent twitchEvent)
    {
        if (string.IsNullOrWhiteSpace(template))
        {
            return "";
        }

        var userName = string.IsNullOrWhiteSpace(twitchEvent.UserName)
            ? "Anonimo"
            : twitchEvent.UserName;
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["user"] = userName,
            ["bits"] = (twitchEvent.Bits ?? 0).ToString(),
            ["reward"] = twitchEvent.RewardTitle ?? "",
            ["viewers"] = (twitchEvent.ViewerCount ?? 0).ToString(),
            ["message"] = twitchEvent.Message ?? "",
            ["event"] = DisplayNames.For(twitchEvent.Kind)
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
        _http.Dispose();
    }
}
