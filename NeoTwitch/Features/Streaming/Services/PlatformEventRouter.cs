using NeoTwitch.Models;

namespace NeoTwitch.Services.Streaming;

/// <summary>
/// Central ingress for all platform providers. Event identities are scoped by platform so a
/// Twitch and a YouTube event with the same provider-local identifier never suppress each other.
/// </summary>
public sealed class PlatformEventRouter
{
    private readonly PlatformEventDeduplicator _deduplicator;

    public PlatformEventRouter(PlatformEventDeduplicator? deduplicator = null)
    {
        _deduplicator = deduplicator ?? new PlatformEventDeduplicator();
    }

    public event Func<StreamEvent, CancellationToken, Task>? EventReceivedAsync;

    public async Task<bool> PublishAsync(StreamEvent streamEvent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(streamEvent);
        if (!_deduplicator.TryAccept(streamEvent))
        {
            return false;
        }

        var handlers = EventReceivedAsync;
        if (handlers is null)
        {
            return true;
        }

        foreach (var handler in handlers.GetInvocationList().Cast<Func<StreamEvent, CancellationToken, Task>>())
        {
            cancellationToken.ThrowIfCancellationRequested();
            await handler(streamEvent, cancellationToken);
        }

        return true;
    }
}
