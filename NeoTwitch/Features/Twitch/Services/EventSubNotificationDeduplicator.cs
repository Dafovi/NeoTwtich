namespace NeoTwitch.Services;

public sealed class EventSubNotificationDeduplicator
{
    public static readonly TimeSpan DefaultTimeToLive = TimeSpan.FromMinutes(10);
    public const int DefaultCapacity = 4096;

    private readonly object _sync = new();
    private readonly TimeProvider _timeProvider;
    private readonly TimeSpan _timeToLive;
    private readonly int _capacity;
    private readonly Dictionary<string, LinkedListNode<Entry>> _entries = new(StringComparer.Ordinal);
    private readonly LinkedList<Entry> _oldestFirst = [];

    public EventSubNotificationDeduplicator(
        TimeProvider? timeProvider = null,
        TimeSpan? timeToLive = null,
        int capacity = DefaultCapacity)
    {
        _timeProvider = timeProvider ?? TimeProvider.System;
        _timeToLive = timeToLive ?? DefaultTimeToLive;
        if (_timeToLive <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(timeToLive));
        }

        if (capacity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity));
        }

        _capacity = capacity;
    }

    public int Count
    {
        get
        {
            lock (_sync)
            {
                RemoveExpired(_timeProvider.GetUtcNow());
                return _entries.Count;
            }
        }
    }

    public bool TryAccept(string messageId)
    {
        if (string.IsNullOrWhiteSpace(messageId))
        {
            return false;
        }

        lock (_sync)
        {
            var now = _timeProvider.GetUtcNow();
            RemoveExpired(now);
            if (_entries.ContainsKey(messageId))
            {
                return false;
            }

            var node = _oldestFirst.AddLast(new Entry(messageId, now));
            _entries.Add(messageId, node);
            while (_entries.Count > _capacity)
            {
                RemoveOldest();
            }

            return true;
        }
    }

    private void RemoveExpired(DateTimeOffset now)
    {
        while (_oldestFirst.First is { } oldest
            && now - oldest.Value.AcceptedAt >= _timeToLive)
        {
            RemoveOldest();
        }
    }

    private void RemoveOldest()
    {
        var oldest = _oldestFirst.First;
        if (oldest is null)
        {
            return;
        }

        _oldestFirst.RemoveFirst();
        _entries.Remove(oldest.Value.MessageId);
    }

    private sealed record Entry(string MessageId, DateTimeOffset AcceptedAt);
}
