using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using NeoTwitch.Models;
using NeoTwitch.Services.Text;
using Protocol = NeoTwitch.Services.TwitchEventSubProtocol;

namespace NeoTwitch.Services;

public sealed class TwitchEventSubSubscriptionRegistrar : IDisposable
{
    private readonly IUiTextService _text;
    private readonly Action<string> _log;
    private readonly HttpClient _http;
    private readonly bool _ownsHttp;

    public TwitchEventSubSubscriptionRegistrar(
        IUiTextService text,
        Action<string> log,
        HttpClient? httpClient = null)
    {
        _text = text;
        _log = log;
        _http = httpClient ?? new HttpClient();
        _ownsHttp = httpClient is null;
    }

    public async Task<EventSubSubscriptionSummary> CreateSubscriptionsAsync(
        string sessionId,
        AppConfig config,
        CancellationToken cancellationToken)
    {
        var definitions = TwitchEventSubSubscriptionPlanner.BuildDefinitions(config);
        var attempts = new List<EventSubSubscriptionAttempt>(definitions.Count);

        if (definitions.Count == 0)
        {
            _log(_text.Get(UiTextKeys.TwitchEventSubNoActiveRulesLog));
            return EventSubSubscriptionSummary.FromAttempts(attempts);
        }

        foreach (var definition in definitions)
        {
            var body = new
            {
                type = definition.Type,
                version = definition.Version,
                condition = definition.Condition,
                transport = new
                {
                    method = Protocol.TransportMethodWebSocket,
                    session_id = sessionId
                }
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, Protocol.SubscriptionsApiUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", config.Token.AccessToken);
            request.Headers.Add("Client-Id", config.TwitchClientId);
            request.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, Protocol.ContentTypeJson);

            try
            {
                using var response = await _http.SendAsync(request, cancellationToken);
                var responseText = await response.Content.ReadAsStringAsync(cancellationToken);
                var diagnostic = LimitDiagnostic(responseText);

                if (response.IsSuccessStatusCode)
                {
                    attempts.Add(new EventSubSubscriptionAttempt(
                        definition.Type,
                        definition.IsRequired,
                        true,
                        (int)response.StatusCode,
                        diagnostic));
                    _log(_text.Format(UiTextKeys.TwitchEventSubSubscriptionReadyLog, definition.Type));
                }
                else
                {
                    attempts.Add(new EventSubSubscriptionAttempt(
                        definition.Type,
                        definition.IsRequired,
                        false,
                        (int)response.StatusCode,
                        diagnostic));
                    _log(_text.Format(UiTextKeys.TwitchEventSubSubscriptionCreateFailureLog, definition.Type, diagnostic));
                }
            }
            catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                attempts.Add(new EventSubSubscriptionAttempt(
                    definition.Type,
                    definition.IsRequired,
                    false,
                    null,
                    ex.Message));
                _log(_text.Format(UiTextKeys.TwitchEventSubSubscriptionCreateFailureLog, definition.Type, ex.Message));
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (HttpRequestException ex)
            {
                attempts.Add(new EventSubSubscriptionAttempt(
                    definition.Type,
                    definition.IsRequired,
                    false,
                    null,
                    ex.Message));
                _log(_text.Format(UiTextKeys.TwitchEventSubSubscriptionCreateFailureLog, definition.Type, ex.Message));
            }
        }

        return EventSubSubscriptionSummary.FromAttempts(attempts);
    }

    private static string LimitDiagnostic(string value) =>
        value.Length <= 500 ? value : $"{value[..500]}...";

    public void Dispose()
    {
        if (_ownsHttp)
        {
            _http.Dispose();
        }
    }
}

public sealed record EventSubSubscriptionAttempt(
    string Type,
    bool IsRequired,
    bool Succeeded,
    int? HttpStatusCode,
    string Diagnostic);

public sealed record EventSubSubscriptionSummary(
    IReadOnlyList<EventSubSubscriptionAttempt> Attempts,
    IReadOnlyList<string> FailedRequiredTypes,
    IReadOnlyList<string> FailedOptionalTypes)
{
    public bool AllRequiredSucceeded => FailedRequiredTypes.Count == 0;

    public static EventSubSubscriptionSummary FromAttempts(
        IEnumerable<EventSubSubscriptionAttempt> attempts)
    {
        var materialized = attempts.ToArray();
        return new EventSubSubscriptionSummary(
            materialized,
            materialized
                .Where(attempt => attempt.IsRequired && !attempt.Succeeded)
                .Select(attempt => attempt.Type)
                .Distinct(StringComparer.Ordinal)
                .ToArray(),
            materialized
                .Where(attempt => !attempt.IsRequired && !attempt.Succeeded)
                .Select(attempt => attempt.Type)
                .Distinct(StringComparer.Ordinal)
                .ToArray());
    }
}
