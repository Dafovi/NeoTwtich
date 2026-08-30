using System.Net.WebSockets;
using System.IO;
using System.Text.Json;
using NeoTwitch.Models;
using NeoTwitch.Services.Text;
using Protocol = NeoTwitch.Services.TwitchEventSubProtocol;

namespace NeoTwitch.Services;

public sealed class TwitchEventSubClient : IDisposable, IAsyncDisposable
{
    private readonly TwitchAuthService _authService;
    private readonly Func<AppConfig> _getConfig;
    private readonly Action _saveConfig;
    private readonly Action<string> _log;
    private readonly IUiTextService _text;
    private readonly TwitchEventSubMessageParser _messageParser;
    private readonly TwitchEventSubSubscriptionRegistrar _subscriptionRegistrar;
    private readonly bool _ownsSubscriptionRegistrar;
    private readonly TimeProvider _timeProvider;
    private readonly EventSubNotificationDeduplicator _deduplicator;
    private readonly Func<IEventSubWebSocket> _socketFactory;
    private readonly object _lifecycleSync = new();
    private readonly object _healthSync = new();
    private CancellationTokenSource? _cts;
    private Task? _runner;
    private int _disposed;
    private EventSubConnectionHealthSnapshot _health = new(
        EventSubConnectionHealth.Disconnected,
        "",
        null,
        [],
        "No iniciado");

    public TwitchEventSubClient(
        TwitchAuthService authService,
        Func<AppConfig> getConfig,
        Action saveConfig,
        Action<string> log,
        IUiTextService text,
        TwitchEventSubSubscriptionRegistrar? subscriptionRegistrar = null,
        TimeProvider? timeProvider = null,
        EventSubNotificationDeduplicator? deduplicator = null)
        : this(authService, getConfig, saveConfig, log, text, subscriptionRegistrar, timeProvider, deduplicator, () => new EventSubWebSocket())
    {
    }

    internal TwitchEventSubClient(
        TwitchAuthService authService,
        Func<AppConfig> getConfig,
        Action saveConfig,
        Action<string> log,
        IUiTextService text,
        TwitchEventSubSubscriptionRegistrar? subscriptionRegistrar,
        TimeProvider? timeProvider,
        EventSubNotificationDeduplicator? deduplicator,
        Func<IEventSubWebSocket> socketFactory)
    {
        _authService = authService;
        _getConfig = getConfig;
        _saveConfig = saveConfig;
        _log = log;
        _text = text;
        _messageParser = new TwitchEventSubMessageParser(text);
        _subscriptionRegistrar = subscriptionRegistrar ?? new TwitchEventSubSubscriptionRegistrar(text, log);
        _ownsSubscriptionRegistrar = subscriptionRegistrar is null;
        _timeProvider = timeProvider ?? TimeProvider.System;
        _deduplicator = deduplicator ?? new EventSubNotificationDeduplicator(_timeProvider);
        _socketFactory = socketFactory;
    }

    public event Func<TwitchEvent, CancellationToken, Task>? EventReceivedAsync;

    public event Action<EventSubConnectionHealthSnapshot>? HealthChanged;

    public bool IsRunning => _runner is { IsCompleted: false };

    public EventSubConnectionHealthSnapshot Health
    {
        get
        {
            lock (_healthSync)
            {
                return _health;
            }
        }
    }

    public bool IsHealthy => Health.IsFullyHealthy;

    public Task StartAsync()
    {
        lock (_lifecycleSync)
        {
            if (IsRunning)
            {
                return Task.CompletedTask;
            }

            _cts = new CancellationTokenSource();
            SetHealth(EventSubConnectionHealth.Connecting, reason: "Inicio solicitado");
            _runner = Task.Run(() => RunAsync(_cts.Token));
        }

        return Task.CompletedTask;
    }

    public async Task StopAsync()
    {
        CancellationTokenSource? cts;
        Task? runner;
        lock (_lifecycleSync)
        {
            cts = _cts;
            runner = _runner;
        }

        if (cts is null)
        {
            SetHealth(EventSubConnectionHealth.Disconnected, reason: "Detenido");
            return;
        }

        cts.Cancel();

        try
        {
            if (runner is not null)
            {
                await runner;
            }
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            lock (_lifecycleSync)
            {
                if (ReferenceEquals(_cts, cts))
                {
                    _cts.Dispose();
                    _cts = null;
                    _runner = null;
                }
            }

            SetHealth(EventSubConnectionHealth.Disconnected, reason: "Cierre solicitado por la aplicación");
        }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        _cts?.Cancel();
        _cts?.Dispose();
        if (_ownsSubscriptionRegistrar)
        {
            _subscriptionRegistrar.Dispose();
        }
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        var url = Protocol.WebSocketUrl;
        var createSubscriptions = true;
        var subscriptionSummary = EventSubSubscriptionSummary.FromAttempts([]);
        var reconnecting = false;

        while (!cancellationToken.IsCancellationRequested)
        {
            IEventSubWebSocket? activeSocket = null;
            try
            {
                SetHealth(
                    reconnecting ? EventSubConnectionHealth.Reconnecting : EventSubConnectionHealth.Connecting,
                    reason: reconnecting ? "Iniciando intento de reconexión" : "Conectando WebSocket");
                var config = _getConfig();
                ValidateConfig(config);
                await _authService.EnsureValidTokenAsync(config, _log, cancellationToken);
                _saveConfig();

                IEventSubWebSocket socket = _socketFactory();
                activeSocket = socket;
                _log(_text.Get(UiTextKeys.TwitchEventSubConnectingLog));
                await socket.ConnectAsync(new Uri(url), cancellationToken);

                var welcome = await ReceiveWelcomeAsync(socket, cancellationToken);
                if (welcome is null)
                {
                    throw new InvalidOperationException(_text.Get(UiTextKeys.TwitchEventSubClosedBeforeWelcome));
                }

                var session = _messageParser.ReadSessionInfo(welcome);
                var sessionId = session.SessionId;
                var freshness = new EventSubConnectionFreshness(
                    _timeProvider,
                    session.KeepaliveTimeout);
                freshness.MarkMessageReceived();
                _log(_text.Format(UiTextKeys.TwitchEventSubConnectedLog, sessionId));

                if (createSubscriptions)
                {
                    subscriptionSummary = await _subscriptionRegistrar.CreateSubscriptionsAsync(sessionId, config, cancellationToken);
                }

                ApplySubscriptionHealth(sessionId, freshness.LastMessageAt, subscriptionSummary);
                while (!cancellationToken.IsCancellationRequested)
                {
                    var outcome = await ReadMessagesAsync(socket, sessionId, freshness, cancellationToken);
                    if (outcome.Cause != EventSubReconnectCause.ServerRequested)
                    {
                        break;
                    }

                    url = outcome.ReconnectUrl
                        ?? throw new InvalidOperationException("Twitch solicitó reconexión sin reconnect_url.");
                    SetHealth(EventSubConnectionHealth.Reconnecting, sessionId, freshness.LastMessageAt,
                        subscriptionSummary.FailedRequiredTypes, "Twitch solicitó migrar la sesión");

                    var migration = await MigrateSessionAsync(socket, sessionId, freshness, url, cancellationToken);
                    if (!migration.Succeeded)
                    {
                        var oldOutcome = await migration.OldReader!;
                        if (oldOutcome.Cause == EventSubReconnectCause.ServerRequested)
                        {
                            url = oldOutcome.ReconnectUrl ?? url;
                            continue;
                        }

                        break;
                    }

                    socket = migration.Socket!;
                    activeSocket = socket;
                    sessionId = migration.SessionId!;
                    freshness = migration.Freshness!;
                    ApplySubscriptionHealth(sessionId, freshness.LastMessageAt, subscriptionSummary);
                    await CloseAndDisposeAsync(migration.OldSocket!);
                }

                var closeDecision = EventSubReconnectPolicy.Decide(EventSubReconnectCause.NormalClose);
                url = Protocol.WebSocketUrl;
                createSubscriptions = closeDecision.CreateSubscriptions;
                reconnecting = true;
                SetHealth(
                    EventSubConnectionHealth.Reconnecting,
                    sessionId,
                    freshness.LastMessageAt,
                    subscriptionSummary.FailedRequiredTypes,
                    "El WebSocket se cerró; se volverán a crear las suscripciones");
                await Task.Delay(closeDecision.Delay, _timeProvider, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                SetHealth(EventSubConnectionHealth.Disconnected, reason: "Cierre solicitado por la aplicación");
                return;
            }
            catch (EventSubStaleConnectionException ex)
            {
                var decision = EventSubReconnectPolicy.Decide(EventSubReconnectCause.Stale);
                _log($"EventSub: conexión obsoleta ({ex.Message}). Reconectando.");
                url = Protocol.WebSocketUrl;
                createSubscriptions = decision.CreateSubscriptions;
                reconnecting = true;
                SetHealth(EventSubConnectionHealth.Reconnecting, reason: ex.Message);
                await Task.Delay(decision.Delay, _timeProvider, cancellationToken);
            }
            catch (Exception ex)
            {
                _log(_text.Format(UiTextKeys.TwitchEventSubDisconnectedLog, ex.Message));
                SetHealth(EventSubConnectionHealth.Faulted, reason: ex.Message);
                var cause = TwitchConnectionRecoveryService.IsRecoverableRefreshError(ex)
                    ? EventSubReconnectCause.AuthenticationFailure
                    : EventSubReconnectCause.TransientFailure;
                var decision = EventSubReconnectPolicy.Decide(cause);
                url = Protocol.WebSocketUrl;
                createSubscriptions = decision.CreateSubscriptions;
                reconnecting = true;
                SetHealth(
                    EventSubConnectionHealth.Reconnecting,
                    reason: cause == EventSubReconnectCause.AuthenticationFailure
                        ? $"Reintento tras fallo de autenticación: {ex.Message}"
                        : $"Reintento tras error transitorio: {ex.Message}");
                await Task.Delay(decision.Delay, _timeProvider, cancellationToken);
            }
            finally
            {
                if (activeSocket is not null)
                {
                    await CloseAndDisposeAsync(activeSocket);
                }
            }
        }

        SetHealth(EventSubConnectionHealth.Disconnected, reason: "Cierre solicitado por la aplicación");
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

    private async Task<EventSubReceiveOutcome> ReadMessagesAsync(
        IEventSubWebSocket socket,
        string sessionId,
        EventSubConnectionFreshness freshness,
        CancellationToken cancellationToken)
    {
        while (socket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
        {
            var message = await ReceiveTextWithWatchdogAsync(socket, sessionId, freshness, cancellationToken);
            if (message is null)
            {
                return new EventSubReceiveOutcome(EventSubReconnectCause.NormalClose, null);
            }

            using var doc = JsonDocument.Parse(message);
            var root = doc.RootElement;
            var metadata = root.GetProperty(Protocol.Json.Metadata);
            var messageType = metadata.GetProperty(Protocol.Json.MessageType).GetString();
            var messageId = metadata.TryGetProperty(Protocol.Json.MessageId, out var messageIdElement)
                ? messageIdElement.GetString() ?? ""
                : "";
            UpdateLastMessage(sessionId, freshness.LastMessageAt);

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
                    return new EventSubReceiveOutcome(EventSubReconnectCause.ServerRequested, reconnectUrl);
                case Protocol.MessageTypes.Notification:
                    if (string.IsNullOrWhiteSpace(messageId))
                    {
                        throw new InvalidOperationException("La notificación EventSub no contiene metadata.message_id.");
                    }

                    if (!_deduplicator.TryAccept(messageId))
                    {
                        _log($"EventSub: duplicado suprimido. Sesión {sessionId}, mensaje {messageId}.");
                        break;
                    }

                    var twitchEvent = _messageParser.ParseEvent(root.GetProperty(Protocol.Json.Payload));
                    if (twitchEvent is not null)
                    {
                        twitchEvent.EventSubMessageId = messageId;
                        twitchEvent.EventSubSessionId = sessionId;
                        twitchEvent.EventSubMessageType = messageType;
                        await DispatchEventAsync(twitchEvent, cancellationToken);
                    }

                    break;
                case Protocol.MessageTypes.Revocation:
                    _log(_text.Format(UiTextKeys.TwitchEventSubRevokedLog, message));
                    var revokedType = root.GetProperty(Protocol.Json.Payload)
                        .GetProperty(Protocol.Json.Subscription)
                        .GetProperty(Protocol.Json.Type)
                        .GetString() ?? "desconocida";
                    SetHealth(
                        EventSubConnectionHealth.Degraded,
                        sessionId,
                        freshness.LastMessageAt,
                        [revokedType],
                        $"Twitch revocó la suscripción {revokedType}");
                    break;
                default:
                    _log(_text.Format(UiTextKeys.TwitchEventSubUnknownMessageLog, messageType ?? string.Empty));
                    break;
            }
        }

        return new EventSubReceiveOutcome(EventSubReconnectCause.NormalClose, null);
    }

    private async Task<string?> ReceiveTextWithWatchdogAsync(
        IEventSubWebSocket socket,
        string sessionId,
        EventSubConnectionFreshness freshness,
        CancellationToken cancellationToken)
    {
        using var receiveCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var receiveTask = ReceiveTextAsync(socket, receiveCts.Token);
        while (true)
        {
            var watchdogTask = Task.Delay(freshness.RemainingUntilStale, _timeProvider, receiveCts.Token);
            var completed = await Task.WhenAny(receiveTask, watchdogTask);
            if (completed == receiveTask)
            {
                var message = await receiveTask;
                receiveCts.Cancel();
                if (message is not null)
                {
                    freshness.MarkMessageReceived();
                }

                return message;
            }

            cancellationToken.ThrowIfCancellationRequested();
            if (freshness.TryDetectStale())
            {
                break;
            }
        }

        var age = freshness.CurrentAge;
        SetHealth(
            EventSubConnectionHealth.Stale,
            sessionId,
            freshness.LastMessageAt,
            Health.FailedSubscriptionTypes,
            $"Sin mensajes válidos durante {age.TotalSeconds:F0} segundos");
        receiveCts.Cancel();
        socket.Abort();
        try
        {
            await receiveTask;
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex) when (ex is WebSocketException or ObjectDisposedException or InvalidOperationException)
        {
        }

        throw new EventSubStaleConnectionException(
            $"último mensaje hace {age.TotalSeconds:F0}s; sesión {sessionId}");
    }

    private static async Task<string?> ReceiveWelcomeAsync(
        IEventSubWebSocket socket,
        CancellationToken cancellationToken)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(45));
        return await ReceiveTextAsync(socket, timeoutCts.Token);
    }

    private async Task DispatchEventAsync(TwitchEvent twitchEvent, CancellationToken cancellationToken)
    {
        var handlers = EventReceivedAsync;
        if (handlers is null)
        {
            return;
        }

        foreach (var handler in handlers.GetInvocationList().Cast<Func<TwitchEvent, CancellationToken, Task>>())
        {
            await handler(twitchEvent, cancellationToken);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        await StopAsync();
        if (_ownsSubscriptionRegistrar)
        {
            _subscriptionRegistrar.Dispose();
        }
    }

    private void ApplySubscriptionHealth(
        string sessionId,
        DateTimeOffset lastMessageAt,
        EventSubSubscriptionSummary summary)
    {
        var failedTypes = summary.FailedRequiredTypes;
        var optionalFailures = summary.FailedOptionalTypes;
        _log(
            $"EventSub: sesión {sessionId}; suscripciones {summary.Attempts.Count - failedTypes.Count - optionalFailures.Count}/{summary.Attempts.Count} correctas; "
            + $"requeridas fallidas: {(failedTypes.Count == 0 ? "ninguna" : string.Join(", ", failedTypes))}.");

        var resolvedHealth = EventSubConnectionHealthResolver.FromSubscriptions(summary);
        if (resolvedHealth == EventSubConnectionHealth.Degraded)
        {
            SetHealth(
                EventSubConnectionHealth.Degraded,
                sessionId,
                lastMessageAt,
                failedTypes,
                "Una o más suscripciones requeridas fallaron");
            return;
        }

        SetHealth(
            EventSubConnectionHealth.Connected,
            sessionId,
            lastMessageAt,
            [],
            optionalFailures.Count == 0
                ? "Socket y suscripciones requeridas saludables"
                : $"Suscripciones opcionales fallidas: {string.Join(", ", optionalFailures)}");
    }

    private void UpdateLastMessage(string sessionId, DateTimeOffset lastMessageAt)
    {
        var current = Health;
        SetHealth(
            current.State,
            sessionId,
            lastMessageAt,
            current.FailedSubscriptionTypes,
            current.Reason);
    }

    private void SetHealth(
        EventSubConnectionHealth state,
        string sessionId = "",
        DateTimeOffset? lastMessageAt = null,
        IReadOnlyList<string>? failedSubscriptionTypes = null,
        string reason = "")
    {
        EventSubConnectionHealthSnapshot snapshot;
        EventSubConnectionHealthSnapshot previous;
        lock (_healthSync)
        {
            previous = _health;
            snapshot = new EventSubConnectionHealthSnapshot(
                state,
                sessionId,
                lastMessageAt,
                failedSubscriptionTypes?.ToArray() ?? [],
                reason);
            _health = snapshot;
        }

        if (previous.State != snapshot.State)
        {
            var age = snapshot.LastMessageAt is { } observedAt
                ? $"; último mensaje hace {Math.Max(0, (_timeProvider.GetUtcNow() - observedAt).TotalSeconds):F0}s"
                : "";
            _log(
                $"EventSub: salud {previous.State} -> {snapshot.State}; "
                + $"sesión {(string.IsNullOrWhiteSpace(snapshot.SessionId) ? "ninguna" : snapshot.SessionId)}{age}; "
                + $"motivo: {(string.IsNullOrWhiteSpace(snapshot.Reason) ? "no especificado" : snapshot.Reason)}.");
        }

        var handlers = HealthChanged;
        if (handlers is null)
        {
            return;
        }

        foreach (var handler in handlers.GetInvocationList().Cast<Action<EventSubConnectionHealthSnapshot>>())
        {
            try
            {
                handler(snapshot);
            }
            catch (Exception ex)
            {
                _log($"EventSub: observador de salud falló: {ex.Message}");
            }
        }
    }

    internal async Task<EventSubMigrationResult> MigrateSessionAsync(
        IEventSubWebSocket oldSocket,
        string oldSessionId,
        EventSubConnectionFreshness oldFreshness,
        string reconnectUrl,
        CancellationToken cancellationToken)
    {
        var overlapCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var oldReader = ReadMessagesAsync(oldSocket, oldSessionId, oldFreshness, overlapCts.Token);
        var newSocket = _socketFactory();
        try
        {
            await newSocket.ConnectAsync(new Uri(reconnectUrl), cancellationToken);
            var welcome = await ReceiveWelcomeAsync(newSocket, cancellationToken)
                ?? throw new InvalidOperationException(_text.Get(UiTextKeys.TwitchEventSubClosedBeforeWelcome));
            var session = _messageParser.ReadSessionInfo(welcome);
            var freshness = new EventSubConnectionFreshness(_timeProvider, session.KeepaliveTimeout);
            freshness.MarkMessageReceived();

            overlapCts.Cancel();
            await ObserveCanceledOverlapAsync(oldReader);
            overlapCts.Dispose();
            return EventSubMigrationResult.Success(newSocket, oldSocket, session.SessionId, freshness);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            overlapCts.Cancel();
            await ObserveCanceledOverlapAsync(oldReader);
            overlapCts.Dispose();
            await newSocket.DisposeAsync();
            throw;
        }
        catch (Exception ex)
        {
            await newSocket.DisposeAsync();
            _log($"EventSub: la migración no pudo establecer la nueva sesión; se conserva la anterior ({ex.Message}).");
            _ = oldReader.ContinueWith(_ => overlapCts.Dispose(), CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
            return EventSubMigrationResult.Failure(oldReader);
        }
    }

    private static async Task ObserveCanceledOverlapAsync(Task<EventSubReceiveOutcome> oldReader)
    {
        try
        {
            await oldReader;
        }
        catch (OperationCanceledException)
        {
        }
    }

    private static async Task CloseAndDisposeAsync(IEventSubWebSocket socket)
    {
        try
        {
            if (socket.State == WebSocketState.Open)
            {
                await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "EventSub session migrated", CancellationToken.None);
            }
        }
        catch (WebSocketException)
        {
        }
        finally
        {
            await socket.DisposeAsync();
        }
    }

    private static async Task<string?> ReceiveTextAsync(IEventSubWebSocket socket, CancellationToken cancellationToken)
    {
        var buffer = new byte[8192];
        var message = new EventSubMessageAccumulator();
        WebSocketReceiveResult result;

        do
        {
            result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);

            if (result.MessageType == WebSocketMessageType.Close)
            {
                return null;
            }

            try
            {
                message.Append(buffer.AsSpan(0, result.Count));
            }
            catch (EventSubMessageTooLargeException)
            {
                socket.Abort();
                throw;
            }
        }
        while (!result.EndOfMessage);

        return message.GetText();
    }
}

internal sealed record EventSubMigrationResult(
    bool Succeeded,
    IEventSubWebSocket? Socket,
    string? SessionId,
    EventSubConnectionFreshness? Freshness,
    IEventSubWebSocket? OldSocket,
    Task<EventSubReceiveOutcome>? OldReader)
{
    public static EventSubMigrationResult Success(IEventSubWebSocket socket, IEventSubWebSocket oldSocket,
        string sessionId, EventSubConnectionFreshness freshness) =>
        new(true, socket, sessionId, freshness, oldSocket, null);

    public static EventSubMigrationResult Failure(Task<EventSubReceiveOutcome> oldReader) =>
        new(false, null, null, null, null, oldReader);
}

internal sealed record EventSubReceiveOutcome(EventSubReconnectCause Cause, string? ReconnectUrl);

internal sealed class EventSubStaleConnectionException(string message) : IOException(message);
