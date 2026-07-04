using System.Net.Http.Headers;
using System.Net.Http;
using System.Net.WebSockets;
using System.IO;
using System.Text;
using System.Text.Json;
using NeoTwitch.Models;
using NeoTwitch.Services.Text;
using Protocol = NeoTwitch.Services.TwitchEventSubProtocol;

namespace NeoTwitch.Services;

public sealed class TwitchEventSubClient : IDisposable
{
    private readonly TwitchAuthService _authService;
    private readonly Func<AppConfig> _getConfig;
    private readonly Action _saveConfig;
    private readonly Action<string> _log;
    private readonly IUiTextService _text;
    private readonly TwitchEventSubMessageParser _messageParser;
    private readonly HttpClient _http = new();
    private CancellationTokenSource? _cts;
    private Task? _runner;

    public TwitchEventSubClient(
        TwitchAuthService authService,
        Func<AppConfig> getConfig,
        Action saveConfig,
        Action<string> log,
        IUiTextService text)
    {
        _authService = authService;
        _getConfig = getConfig;
        _saveConfig = saveConfig;
        _log = log;
        _text = text;
        _messageParser = new TwitchEventSubMessageParser(text);
    }

    public event Action<TwitchEvent>? EventReceived;

    public bool IsRunning => _runner is { IsCompleted: false };

    public Task StartAsync()
    {
        if (IsRunning)
        {
            return Task.CompletedTask;
        }

        _cts = new CancellationTokenSource();
        _runner = Task.Run(() => RunAsync(_cts.Token));
        return Task.CompletedTask;
    }

    public async Task StopAsync()
    {
        if (_cts is null)
        {
            return;
        }

        _cts.Cancel();

        try
        {
            if (_runner is not null)
            {
                await _runner;
            }
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            _cts.Dispose();
            _cts = null;
            _runner = null;
        }
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _http.Dispose();
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        var url = Protocol.WebSocketUrl;
        var createSubscriptions = true;

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var config = _getConfig();
                ValidateConfig(config);
                await _authService.EnsureValidTokenAsync(config, _log, cancellationToken);
                _saveConfig();

                using var socket = new ClientWebSocket();
                _log(_text.Get(UiTextKeys.TwitchEventSubConnectingLog));
                await socket.ConnectAsync(new Uri(url), cancellationToken);

                var welcome = await ReceiveTextAsync(socket, cancellationToken);
                if (welcome is null)
                {
                    throw new InvalidOperationException(_text.Get(UiTextKeys.TwitchEventSubClosedBeforeWelcome));
                }

                var sessionId = _messageParser.ReadSessionId(welcome);
                _log(_text.Format(UiTextKeys.TwitchEventSubConnectedLog, sessionId));

                if (createSubscriptions)
                {
                    await CreateSubscriptionsAsync(sessionId, config, cancellationToken);
                }

                var reconnect = await ReadMessagesAsync(socket, cancellationToken);
                if (reconnect is not null)
                {
                    url = reconnect;
                    createSubscriptions = false;
                    continue;
                }

                url = Protocol.WebSocketUrl;
                createSubscriptions = true;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _log(_text.Format(UiTextKeys.TwitchEventSubDisconnectedLog, ex.Message));
                url = Protocol.WebSocketUrl;
                createSubscriptions = true;
                await Task.Delay(TimeSpan.FromSeconds(8), cancellationToken);
            }
        }
    }

    private void ValidateConfig(AppConfig config)
    {
        if (string.IsNullOrWhiteSpace(config.TwitchClientId))
        {
            throw new InvalidOperationException(_text.Get(UiTextKeys.TwitchEventSubMissingClientId));
        }

        if (!config.Token.HasToken)
        {
            throw new InvalidOperationException(_text.Get(UiTextKeys.TwitchEventSubMissingLogin));
        }

        if (!config.Channel.IsReady)
        {
            throw new InvalidOperationException(_text.Get(UiTextKeys.TwitchEventSubMissingUser));
        }
    }

    private async Task<string?> ReadMessagesAsync(ClientWebSocket socket, CancellationToken cancellationToken)
    {
        while (socket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
        {
            var message = await ReceiveTextAsync(socket, cancellationToken);
            if (message is null)
            {
                return null;
            }

            using var doc = JsonDocument.Parse(message);
            var root = doc.RootElement;
            var messageType = root.GetProperty(Protocol.Json.Metadata).GetProperty(Protocol.Json.MessageType).GetString();

            switch (messageType)
            {
                case Protocol.MessageTypes.KeepAlive:
                    break;
                case Protocol.MessageTypes.Reconnect:
                    var reconnectUrl = root
                        .GetProperty(Protocol.Json.Payload)
                        .GetProperty(Protocol.Json.Session)
                        .GetProperty(Protocol.Json.ReconnectUrl)
                        .GetString();

                    _log(_text.Get(UiTextKeys.TwitchEventSubReconnectRequestedLog));
                    return reconnectUrl;
                case Protocol.MessageTypes.Notification:
                    var twitchEvent = _messageParser.ParseEvent(root.GetProperty(Protocol.Json.Payload));
                    if (twitchEvent is not null)
                    {
                        EventReceived?.Invoke(twitchEvent);
                    }

                    break;
                case Protocol.MessageTypes.Revocation:
                    _log(_text.Format(UiTextKeys.TwitchEventSubRevokedLog, message));
                    break;
                default:
                    _log(_text.Format(UiTextKeys.TwitchEventSubUnknownMessageLog, messageType ?? string.Empty));
                    break;
            }
        }

        return null;
    }

    private async Task CreateSubscriptionsAsync(string sessionId, AppConfig config, CancellationToken cancellationToken)
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

    private static async Task<string?> ReceiveTextAsync(ClientWebSocket socket, CancellationToken cancellationToken)
    {
        var buffer = new byte[8192];
        using var stream = new MemoryStream();
        WebSocketReceiveResult result;

        do
        {
            result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);

            if (result.MessageType == WebSocketMessageType.Close)
            {
                return null;
            }

            stream.Write(buffer, 0, result.Count);
        }
        while (!result.EndOfMessage);

        return Encoding.UTF8.GetString(stream.ToArray());
    }
}
