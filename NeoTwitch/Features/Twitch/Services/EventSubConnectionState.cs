namespace NeoTwitch.Services;

public enum EventSubConnectionHealth
{
    Disconnected,
    Connecting,
    Connected,
    Degraded,
    Reconnecting,
    Stale,
    Faulted
}

public sealed record EventSubConnectionHealthSnapshot(
    EventSubConnectionHealth State,
    string SessionId,
    DateTimeOffset? LastMessageAt,
    IReadOnlyList<string> FailedSubscriptionTypes,
    string Reason)
{
    public bool IsFullyHealthy => State == EventSubConnectionHealth.Connected;
}

public sealed class EventSubConnectionFreshness
{
    public static readonly TimeSpan DefaultSafetyMargin = TimeSpan.FromSeconds(10);

    private readonly object _sync = new();
    private readonly TimeProvider _timeProvider;
    private readonly TimeSpan _staleAfter;
    private DateTimeOffset _lastMessageAt;
    private bool _staleReported;

    public EventSubConnectionFreshness(
        TimeProvider timeProvider,
        TimeSpan keepaliveTimeout,
        TimeSpan? safetyMargin = null)
    {
        if (keepaliveTimeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(keepaliveTimeout));
        }

        _timeProvider = timeProvider;
        _staleAfter = keepaliveTimeout + (safetyMargin ?? DefaultSafetyMargin);
        _lastMessageAt = timeProvider.GetUtcNow();
    }

    public DateTimeOffset LastMessageAt
    {
        get
        {
            lock (_sync)
            {
                return _lastMessageAt;
            }
        }
    }

    public TimeSpan CurrentAge
    {
        get
        {
            lock (_sync)
            {
                return _timeProvider.GetUtcNow() - _lastMessageAt;
            }
        }
    }

    public TimeSpan RemainingUntilStale
    {
        get
        {
            lock (_sync)
            {
                var remaining = _staleAfter - (_timeProvider.GetUtcNow() - _lastMessageAt);
                return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
            }
        }
    }

    public void MarkMessageReceived()
    {
        lock (_sync)
        {
            _lastMessageAt = _timeProvider.GetUtcNow();
            _staleReported = false;
        }
    }

    public bool TryDetectStale()
    {
        lock (_sync)
        {
            if (_staleReported || _timeProvider.GetUtcNow() - _lastMessageAt < _staleAfter)
            {
                return false;
            }

            _staleReported = true;
            return true;
        }
    }
}

public enum EventSubReconnectCause
{
    NormalClose,
    ServerRequested,
    Stale,
    TransientFailure,
    AuthenticationFailure,
    SubscriptionFailure,
    Shutdown
}

public sealed record EventSubReconnectDecision(
    bool ShouldReconnect,
    bool CreateSubscriptions,
    TimeSpan Delay);

public static class EventSubReconnectPolicy
{
    public static EventSubReconnectDecision Decide(EventSubReconnectCause cause) => cause switch
    {
        EventSubReconnectCause.ServerRequested => new(true, false, TimeSpan.Zero),
        EventSubReconnectCause.NormalClose => new(true, true, TimeSpan.FromSeconds(2)),
        EventSubReconnectCause.Stale => new(true, true, TimeSpan.FromSeconds(2)),
        EventSubReconnectCause.TransientFailure => new(true, true, TimeSpan.FromSeconds(8)),
        EventSubReconnectCause.AuthenticationFailure => new(true, true, TimeSpan.FromSeconds(8)),
        EventSubReconnectCause.SubscriptionFailure => new(false, false, TimeSpan.Zero),
        EventSubReconnectCause.Shutdown => new(false, false, TimeSpan.Zero),
        _ => throw new ArgumentOutOfRangeException(nameof(cause))
    };
}

public static class EventSubConnectionHealthResolver
{
    public static EventSubConnectionHealth FromSubscriptions(EventSubSubscriptionSummary summary) =>
        summary.AllRequiredSucceeded
            ? EventSubConnectionHealth.Connected
            : EventSubConnectionHealth.Degraded;
}
