using NeoTwitch.Models;

namespace NeoTwitch.Services.Streaming;

public sealed class PlatformEventDeduplicator
{
    private readonly object _sync = new();
    private readonly TimeProvider _timeProvider;
    private readonly TimeSpan _timeToLive;
    private readonly int _capacity;
    private readonly Dictionary<string, LinkedListNode<Entry>> _entries = new(StringComparer.Ordinal);
    private readonly LinkedList<Entry> _oldestFirst = [];

    public PlatformEventDeduplicator(
        TimeProvider? timeProvider = null,
        TimeSpan? timeToLive = null,
        int capacity = 4096)
    {
        _timeProvider = timeProvider ?? TimeProvider.System;
        _timeToLive = timeToLive ?? TimeSpan.FromMinutes(10);
        _capacity = capacity;
    }

    public bool TryAccept(StreamEvent streamEvent)
    {
        if (string.IsNullOrWhiteSpace(streamEvent.EventId))
        {
            return true;
        }

        var key = $"{streamEvent.Platform}:{streamEvent.EventId}";
        lock (_sync)
        {
            var now = _timeProvider.GetUtcNow();
            RemoveExpired(now);
            if (_entries.ContainsKey(key))
            {
                return false;
            }

            _entries[key] = _oldestFirst.AddLast(new Entry(key, now));
            while (_entries.Count > _capacity)
            {
                RemoveOldest();
            }

            return true;
        }
    }

    private void RemoveExpired(DateTimeOffset now)
    {
        while (_oldestFirst.First is { } oldest && now - oldest.Value.AcceptedAt >= _timeToLive)
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
        _entries.Remove(oldest.Value.Key);
    }

    private sealed record Entry(string Key, DateTimeOffset AcceptedAt);
}
