using System.Net.Http.Headers;
using System.Net.Http;
using System.Net.WebSockets;
using System.IO;
using System.Text;
using System.Text.Json;
using LucesCanjeTwitch.Models;

namespace LucesCanjeTwitch.Services;

public sealed class TwitchEventSubClient : IDisposable
{
    private const string EventSubWebSocketUrl = "wss://eventsub.wss.twitch.tv/ws?keepalive_timeout_seconds=30";

    private readonly TwitchAuthService _authService;
    private readonly Func<AppConfig> _getConfig;
    private readonly Action _saveConfig;
    private readonly Action<string> _log;
    private readonly HttpClient _http = new();
    private CancellationTokenSource? _cts;
    private Task? _runner;

    public TwitchEventSubClient(
        TwitchAuthService authService,
        Func<AppConfig> getConfig,
        Action saveConfig,
        Action<string> log)
    {
        _authService = authService;
        _getConfig = getConfig;
        _saveConfig = saveConfig;
        _log = log;
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
        var url = EventSubWebSocketUrl;
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
                _log("Conectando a EventSub WebSocket...");
                await socket.ConnectAsync(new Uri(url), cancellationToken);

                var welcome = await ReceiveTextAsync(socket, cancellationToken);
                if (welcome is null)
                {
                    throw new InvalidOperationException("Twitch cerro el WebSocket antes del mensaje de bienvenida.");
                }

                var sessionId = ReadSessionId(welcome);
                _log($"EventSub conectado. Sesion {sessionId}.");

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

                url = EventSubWebSocketUrl;
                createSubscriptions = true;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _log($"Twitch desconectado: {ex.Message}");
                url = EventSubWebSocketUrl;
                createSubscriptions = true;
                await Task.Delay(TimeSpan.FromSeconds(8), cancellationToken);
            }
        }
    }

    private static void ValidateConfig(AppConfig config)
    {
        if (string.IsNullOrWhiteSpace(config.TwitchClientId))
        {
            throw new InvalidOperationException("Falta el Client ID de Twitch.");
        }

        if (!config.Token.HasToken)
        {
            throw new InvalidOperationException("Falta iniciar sesion en Twitch.");
        }

        if (!config.Channel.IsReady)
        {
            throw new InvalidOperationException("Falta leer el usuario de Twitch.");
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
            var messageType = root.GetProperty("metadata").GetProperty("message_type").GetString();

            switch (messageType)
            {
                case "session_keepalive":
                    break;
                case "session_reconnect":
                    var reconnectUrl = root
                        .GetProperty("payload")
                        .GetProperty("session")
                        .GetProperty("reconnect_url")
                        .GetString();

                    _log("Twitch pidio reconectar el WebSocket.");
                    return reconnectUrl;
                case "notification":
                    var twitchEvent = ParseEvent(root.GetProperty("payload"));
                    if (twitchEvent is not null)
                    {
                        EventReceived?.Invoke(twitchEvent);
                    }

                    break;
                case "revocation":
                    _log($"Twitch revoco una suscripcion: {message}");
                    break;
                default:
                    _log($"Mensaje EventSub no reconocido: {messageType}");
                    break;
            }
        }

        return null;
    }

    private async Task CreateSubscriptionsAsync(string sessionId, AppConfig config, CancellationToken cancellationToken)
    {
        var definitions = BuildSubscriptionDefinitions(config)
            .DistinctBy(definition => definition.Type)
            .ToArray();

        if (definitions.Length == 0)
        {
            _log("No hay reglas activas para suscribir en Twitch.");
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
                    method = "websocket",
                    session_id = sessionId
                }
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.twitch.tv/helix/eventsub/subscriptions");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", config.Token.AccessToken);
            request.Headers.Add("Client-Id", config.TwitchClientId);
            request.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

            using var response = await _http.SendAsync(request, cancellationToken);
            var responseText = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _log($"Suscripcion lista: {definition.Type}");
            }
            else
            {
                _log($"No pude crear {definition.Type}: {responseText}");
            }
        }
    }

    private static IEnumerable<EventSubDefinition> BuildSubscriptionDefinitions(AppConfig config)
    {
        var broadcasterId = config.Channel.UserId;
        var activeKinds = config.Rules
            .Where(rule => rule.IsEnabled)
            .Select(rule => rule.EventKind)
            .Where(kind => kind != TwitchEventKind.Test)
            .Distinct()
            .ToArray();

        foreach (var kind in activeKinds)
        {
            yield return kind switch
            {
                TwitchEventKind.Follow => new EventSubDefinition(
                    "channel.follow",
                    "2",
                    new Dictionary<string, string>
                    {
                        ["broadcaster_user_id"] = broadcasterId,
                        ["moderator_user_id"] = broadcasterId
                    }),
                TwitchEventKind.Subscription => new EventSubDefinition(
                    "channel.subscribe",
                    "1",
                    new Dictionary<string, string>
                    {
                        ["broadcaster_user_id"] = broadcasterId
                    }),
                TwitchEventKind.Raid => new EventSubDefinition(
                    "channel.raid",
                    "1",
                    new Dictionary<string, string>
                    {
                        ["to_broadcaster_user_id"] = broadcasterId
                    }),
                TwitchEventKind.Cheer => new EventSubDefinition(
                    "channel.cheer",
                    "1",
                    new Dictionary<string, string>
                    {
                        ["broadcaster_user_id"] = broadcasterId
                    }),
                TwitchEventKind.ChatCommand => new EventSubDefinition(
                    "channel.chat.message",
                    "1",
                    new Dictionary<string, string>
                    {
                        ["broadcaster_user_id"] = broadcasterId,
                        ["user_id"] = broadcasterId
                    }),
                TwitchEventKind.ChannelPointRedemption => new EventSubDefinition(
                    "channel.channel_points_custom_reward_redemption.add",
                    "1",
                    new Dictionary<string, string>
                    {
                        ["broadcaster_user_id"] = broadcasterId
                    }),
                _ => throw new InvalidOperationException($"Evento no soportado: {kind}")
            };
        }
    }

    private static string ReadSessionId(string message)
    {
        using var doc = JsonDocument.Parse(message);
        var root = doc.RootElement;
        var messageType = root.GetProperty("metadata").GetProperty("message_type").GetString();

        if (messageType != "session_welcome")
        {
            throw new InvalidOperationException($"Twitch esperaba session_welcome y envio {messageType}.");
        }

        return root
            .GetProperty("payload")
            .GetProperty("session")
            .GetProperty("id")
            .GetString() ?? throw new InvalidOperationException("Twitch no envio session_id.");
    }

    private static TwitchEvent? ParseEvent(JsonElement payload)
    {
        var type = payload.GetProperty("subscription").GetProperty("type").GetString();
        var eventPayload = payload.GetProperty("event");

        return type switch
        {
            "channel.follow" => new TwitchEvent
            {
                Kind = TwitchEventKind.Follow,
                RawType = type,
                UserName = ReadString(eventPayload, "user_name"),
                Title = $"{ReadString(eventPayload, "user_name")} siguio el canal"
            },
            "channel.subscribe" => new TwitchEvent
            {
                Kind = TwitchEventKind.Subscription,
                RawType = type,
                UserName = ReadString(eventPayload, "user_name"),
                Title = $"{ReadString(eventPayload, "user_name")} se suscribio"
            },
            "channel.raid" => new TwitchEvent
            {
                Kind = TwitchEventKind.Raid,
                RawType = type,
                UserName = ReadString(eventPayload, "from_broadcaster_user_name"),
                ViewerCount = ReadInt(eventPayload, "viewers"),
                Title = $"{ReadString(eventPayload, "from_broadcaster_user_name")} hizo raid con {ReadInt(eventPayload, "viewers") ?? 0} viewers"
            },
            "channel.cheer" => new TwitchEvent
            {
                Kind = TwitchEventKind.Cheer,
                RawType = type,
                UserName = ReadString(eventPayload, "user_name"),
                Bits = ReadInt(eventPayload, "bits"),
                Message = ReadString(eventPayload, "message"),
                Title = $"{ReadCheerUserName(eventPayload)} mando {ReadInt(eventPayload, "bits") ?? 0} bits"
            },
            "channel.chat.message" => new TwitchEvent
            {
                Kind = TwitchEventKind.ChatCommand,
                RawType = type,
                UserName = ReadString(eventPayload, "chatter_user_name"),
                Message = ReadChatMessageText(eventPayload),
                Title = $"{ReadString(eventPayload, "chatter_user_name")} escribio {ReadChatMessageText(eventPayload)}"
            },
            "channel.channel_points_custom_reward_redemption.add" => new TwitchEvent
            {
                Kind = TwitchEventKind.ChannelPointRedemption,
                RawType = type,
                UserName = ReadString(eventPayload, "user_name"),
                RewardTitle = ReadRewardTitle(eventPayload),
                Title = $"{ReadString(eventPayload, "user_name")} canjeo {ReadRewardTitle(eventPayload)}"
            },
            _ => null
        };
    }

    private static string? ReadChatMessageText(JsonElement eventPayload)
    {
        if (!eventPayload.TryGetProperty("message", out var message))
        {
            return null;
        }

        return ReadString(message, "text");
    }

    private static string? ReadRewardTitle(JsonElement eventPayload)
    {
        if (!eventPayload.TryGetProperty("reward", out var reward))
        {
            return null;
        }

        return ReadString(reward, "title");
    }

    private static string ReadCheerUserName(JsonElement eventPayload)
    {
        var userName = ReadString(eventPayload, "user_name");
        return string.IsNullOrWhiteSpace(userName) ? "Anonimo" : userName;
    }

    private static string? ReadString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var value) ? value.GetString() : null;
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

    private sealed record EventSubDefinition(string Type, string Version, Dictionary<string, string> Condition);
}
