using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using NeoTwitch.Models;
using NeoTwitch.Services.Text;

namespace NeoTwitch.Services;

public sealed class AlexaRelayService
{
    private readonly IUiTextService _text;
    private readonly HttpClient _http;
    private readonly TimeProvider _timeProvider;

    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    public AlexaRelayService(IUiTextService text, TimeProvider timeProvider, HttpClient? httpClient = null)
    {
        _text = text;
        _timeProvider = timeProvider;
        _http = httpClient ?? new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(10)
        };
    }

    public async Task SendRuleEventAsync(AppConfig config, EventRule rule, TwitchEvent twitchEvent, CancellationToken cancellationToken)
    {
        if (!config.Alexa.IsConfigured || !rule.SendAlexaEvent)
        {
            return;
        }

        var eventName = ResolveEventName(rule, twitchEvent);
        var payload = new AlexaRelayPayload(
            _text.Get(UiTextKeys.AlexaRelaySource),
            eventName,
            rule.Name,
            DisplayNameService.For(twitchEvent.Kind, _text),
            twitchEvent.UserName ?? "",
            twitchEvent.RewardTitle ?? "",
            twitchEvent.Bits,
            twitchEvent.ViewerCount,
            twitchEvent.Message ?? "",
            twitchEvent.Title,
            _timeProvider.GetUtcNow());

        await SendPayloadAsync(config, payload, cancellationToken);
    }

    public async Task SendTestEventAsync(AppConfig config, CancellationToken cancellationToken)
    {
        if (!config.Alexa.IsConfigured)
        {
            throw new InvalidOperationException(_text.Get(UiTextKeys.AlexaRelayConfigureFirst));
        }

        var payload = new AlexaRelayPayload(
            _text.Get(UiTextKeys.AlexaRelaySource),
            _text.Get(UiTextKeys.AlexaRelayTestEventName),
            _text.Get(UiTextKeys.AlexaRelayTestRuleName),
            _text.Get(UiTextKeys.AlexaRelayTestEventKind),
            _text.Get(UiTextKeys.AlexaRelayTestUserName),
            "",
            null,
            null,
            "",
            _text.Get(UiTextKeys.AlexaRelayTestTitle),
            _timeProvider.GetUtcNow());

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
            _text.Get(UiTextKeys.AlexaRelaySource),
            eventName.Trim(),
            _text.Get(UiTextKeys.AlexaRelayBackgroundRuleName),
            _text.Get(UiTextKeys.AlexaRelayBackgroundKind),
            _text.Get(UiTextKeys.AlexaRelayTestUserName),
            "",
            null,
            null,
            "",
            title,
            _timeProvider.GetUtcNow());

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
            throw new InvalidOperationException(_text.Format(UiTextKeys.AlexaRelayResponseFailure, (int)response.StatusCode, responseText));
        }
    }

    private string ResolveEventName(EventRule rule, TwitchEvent twitchEvent)
    {
        return string.IsNullOrWhiteSpace(rule.Name)
            ? DisplayNameService.For(twitchEvent.Kind, _text)
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
