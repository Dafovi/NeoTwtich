using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using LucesCanjeTwitch.Models;

namespace LucesCanjeTwitch.Services;

public sealed class AlexaRelayService
{
    private readonly HttpClient _http = new()
    {
        Timeout = TimeSpan.FromSeconds(10)
    };

    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    public async Task SendRuleEventAsync(AppConfig config, EventRule rule, TwitchEvent twitchEvent, CancellationToken cancellationToken)
    {
        if (!config.Alexa.IsConfigured || !rule.SendAlexaEvent)
        {
            return;
        }

        var eventName = ResolveEventName(rule, twitchEvent);
        var payload = new AlexaRelayPayload(
            "neo-twitch",
            eventName,
            rule.Name,
            DisplayNames.For(twitchEvent.Kind),
            twitchEvent.UserName ?? "",
            twitchEvent.RewardTitle ?? "",
            twitchEvent.Bits,
            twitchEvent.ViewerCount,
            twitchEvent.Message ?? "",
            twitchEvent.Title,
            DateTimeOffset.UtcNow);

        await SendPayloadAsync(config, payload, cancellationToken);
    }

    public async Task SendTestEventAsync(AppConfig config, CancellationToken cancellationToken)
    {
        if (!config.Alexa.IsConfigured)
        {
            throw new InvalidOperationException("Activa Alexa y configura la URL del relay primero.");
        }

        var payload = new AlexaRelayPayload(
            "neo-twitch",
            "seguidor",
            "Prueba Alexa",
            "Prueba manual",
            "NeoTwitch",
            "",
            null,
            null,
            "",
            "Prueba de integracion Alexa",
            DateTimeOffset.UtcNow);

        await SendPayloadAsync(config, payload, cancellationToken);
    }

    public async Task SendBackgroundEventAsync(AppConfig config, string eventName, string title, CancellationToken cancellationToken)
    {
        if (!config.Alexa.IsConfigured)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(eventName))
        {
            return;
        }

        var payload = new AlexaRelayPayload(
            "neo-twitch",
            eventName.Trim(),
            "Fondo Alexa",
            "Fondo",
            "NeoTwitch",
            "",
            null,
            null,
            "",
            title,
            DateTimeOffset.UtcNow);

        await SendPayloadAsync(config, payload, cancellationToken);
    }

    private async Task SendPayloadAsync(AppConfig config, AlexaRelayPayload payload, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, config.Alexa.RelayUrl);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        if (!string.IsNullOrWhiteSpace(config.Alexa.AuthToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", config.Alexa.AuthToken.Trim());
        }

        var json = JsonSerializer.Serialize(payload, _jsonOptions);
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");

        using var response = await _http.SendAsync(request, cancellationToken);
        var responseText = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Alexa relay respondio {(int)response.StatusCode}: {responseText}");
        }
    }

    private static string ResolveEventName(EventRule rule, TwitchEvent twitchEvent)
    {
        return string.IsNullOrWhiteSpace(rule.Name)
            ? DisplayNames.For(twitchEvent.Kind)
            : rule.Name.Trim();
    }

    private sealed record AlexaRelayPayload(
        string Source,
        string EventName,
        string RuleName,
        string EventKind,
        string UserName,
        string RewardTitle,
        int? Bits,
        int? ViewerCount,
        string Message,
        string Title,
        DateTimeOffset OccurredAt);
}
