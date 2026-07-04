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

    public async Task CreateSubscriptionsAsync(string sessionId, AppConfig config, CancellationToken cancellationToken)
    {
        var definitions = TwitchEventSubSubscriptionPlanner.BuildDefinitions(config);

        if (definitions.Count == 0)
        {
            _log(_text.Get(UiTextKeys.TwitchEventSubNoActiveRulesLog));
            return;
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

            using var response = await _http.SendAsync(request, cancellationToken);
            var responseText = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _log(_text.Format(UiTextKeys.TwitchEventSubSubscriptionReadyLog, definition.Type));
            }
            else
            {
                _log(_text.Format(UiTextKeys.TwitchEventSubSubscriptionCreateFailureLog, definition.Type, responseText));
            }
        }
    }

    public void Dispose()
    {
        if (_ownsHttp)
        {
            _http.Dispose();
        }
    }
}
