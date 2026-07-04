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

                var sessionId = ReadSessionId(welcome);
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
                    var twitchEvent = ParseEvent(root.GetProperty(Protocol.Json.Payload));
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

    private string ReadSessionId(string message)
    {
        using var doc = JsonDocument.Parse(message);
        var root = doc.RootElement;
        var messageType = root.GetProperty(Protocol.Json.Metadata).GetProperty(Protocol.Json.MessageType).GetString();

        if (messageType != Protocol.MessageTypes.Welcome)
        {
            throw new InvalidOperationException(_text.Format(UiTextKeys.TwitchEventSubExpectedWelcomeFailure, Protocol.MessageTypes.Welcome, messageType ?? string.Empty));
        }

        return root
            .GetProperty(Protocol.Json.Payload)
            .GetProperty(Protocol.Json.Session)
            .GetProperty(Protocol.Json.Id)
            .GetString() ?? throw new InvalidOperationException(_text.Get(UiTextKeys.TwitchEventSubMissingSessionId));
    }

    private TwitchEvent? ParseEvent(JsonElement payload)
    {
        var type = payload.GetProperty(Protocol.Json.Subscription).GetProperty(Protocol.Json.Type).GetString();
        var eventPayload = payload.GetProperty(Protocol.Json.Event);

        return type switch
        {
            Protocol.Events.Follow => new TwitchEvent
            {
                Kind = TwitchEventKind.Follow,
                RawType = type,
                UserName = ReadString(eventPayload, Protocol.EventFields.UserName),
                Title = _text.Format(UiTextKeys.TwitchEventSubTitleFollow, ReadStringOrEmpty(eventPayload, Protocol.EventFields.UserName))
            },
            Protocol.Events.Subscribe => new TwitchEvent
            {
                Kind = TwitchEventKind.Subscription,
                RawType = type,
                UserName = ReadString(eventPayload, Protocol.EventFields.UserName),
                Title = _text.Format(UiTextKeys.TwitchEventSubTitleSubscribe, ReadStringOrEmpty(eventPayload, Protocol.EventFields.UserName))
            },
            Protocol.Events.SubscriptionMessage => new TwitchEvent
            {
                Kind = TwitchEventKind.Subscription,
                RawType = type,
                UserName = ReadString(eventPayload, Protocol.EventFields.UserName),
                Message = ReadSubscriptionMessage(eventPayload),
                Title = _text.Format(UiTextKeys.TwitchEventSubTitleSubscriptionRenew, ReadStringOrEmpty(eventPayload, Protocol.EventFields.UserName))
            },
            Protocol.Events.SubscriptionGift => new TwitchEvent
            {
                Kind = TwitchEventKind.Subscription,
                RawType = type,
                UserName = ReadSubscriptionGiftUserName(eventPayload),
                ViewerCount = ReadInt(eventPayload, Protocol.EventFields.Total),
                Title = _text.Format(UiTextKeys.TwitchEventSubTitleSubscriptionGift, ReadSubscriptionGiftUserName(eventPayload), ReadInt(eventPayload, Protocol.EventFields.Total) ?? 1)
            },
            Protocol.Events.Raid => new TwitchEvent
            {
                Kind = TwitchEventKind.Raid,
                RawType = type,
                UserName = ReadString(eventPayload, Protocol.EventFields.FromBroadcasterUserName),
                ViewerCount = ReadInt(eventPayload, Protocol.EventFields.Viewers),
                Title = _text.Format(UiTextKeys.TwitchEventSubTitleRaid, ReadStringOrEmpty(eventPayload, Protocol.EventFields.FromBroadcasterUserName), ReadInt(eventPayload, Protocol.EventFields.Viewers) ?? 0)
            },
            Protocol.Events.Cheer => new TwitchEvent
            {
                Kind = TwitchEventKind.Cheer,
                RawType = type,
                UserName = ReadString(eventPayload, Protocol.EventFields.UserName),
                Bits = ReadInt(eventPayload, Protocol.EventFields.Bits),
                Message = ReadString(eventPayload, Protocol.Json.Message),
                Title = _text.Format(UiTextKeys.TwitchEventSubTitleCheer, ReadCheerUserName(eventPayload), ReadInt(eventPayload, Protocol.EventFields.Bits) ?? 0)
            },
            Protocol.Events.ChatMessage => new TwitchEvent
            {
                Kind = TwitchEventKind.ChatCommand,
                RawType = type,
                UserName = ReadString(eventPayload, Protocol.EventFields.ChatterUserName),
                Message = ReadChatMessageText(eventPayload),
                Title = _text.Format(UiTextKeys.TwitchEventSubTitleChatMessage, ReadStringOrEmpty(eventPayload, Protocol.EventFields.ChatterUserName), ReadChatMessageText(eventPayload) ?? string.Empty)
            },
            Protocol.Events.ChannelPointRedemption => new TwitchEvent
            {
                Kind = TwitchEventKind.ChannelPointRedemption,
                RawType = type,
                UserName = ReadString(eventPayload, Protocol.EventFields.UserName),
                RewardTitle = ReadRewardTitle(eventPayload),
                Title = _text.Format(UiTextKeys.TwitchEventSubTitleRedemption, ReadStringOrEmpty(eventPayload, Protocol.EventFields.UserName), ReadRewardTitle(eventPayload) ?? string.Empty)
            },
            _ => null
        };
    }

    private static string? ReadChatMessageText(JsonElement eventPayload)
    {
        if (!eventPayload.TryGetProperty(Protocol.Json.Message, out var message))
        {
            return null;
        }

        return ReadString(message, Protocol.Json.Text);
    }

    private static string? ReadSubscriptionMessage(JsonElement eventPayload)
    {
        return eventPayload.TryGetProperty(Protocol.Json.Message, out var message)
            && message.TryGetProperty(Protocol.Json.Text, out var text)
            ? text.GetString()
            : null;
    }

    private string ReadSubscriptionGiftUserName(JsonElement eventPayload)
    {
        var userName = ReadString(eventPayload, Protocol.EventFields.UserName);
        return string.IsNullOrWhiteSpace(userName) ? _text.Get(UiTextKeys.TwitchEventSubAnonymousGiftUser) : userName;
    }

    private static string? ReadRewardTitle(JsonElement eventPayload)
    {
        if (!eventPayload.TryGetProperty(Protocol.Json.Reward, out var reward))
        {
            return null;
        }

        return ReadString(reward, Protocol.Json.Title);
    }

    private string ReadCheerUserName(JsonElement eventPayload)
    {
        var userName = ReadString(eventPayload, Protocol.EventFields.UserName);
        return string.IsNullOrWhiteSpace(userName) ? _text.Get(UiTextKeys.TwitchEventSubAnonymousCheerUser) : userName;
    }

    private static string? ReadString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var value) ? value.GetString() : null;
    }

    private static string ReadStringOrEmpty(JsonElement element, string propertyName)
    {
        return ReadString(element, propertyName) ?? string.Empty;
    }

    private static int? ReadInt(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var value) && value.TryGetInt32(out var number)
            ? number
            : null;
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
