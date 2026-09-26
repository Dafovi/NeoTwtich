using NeoTwitch.Models;

namespace NeoTwitch.Services.Streaming;

/// <summary>
/// Adapts the existing local EventSub client to Neo Stream's provider contract.
/// The EventSub client remains owned by the application composition root.
/// </summary>
public sealed class TwitchStreamingPlatformProvider : IStreamingPlatformProvider
{
    private readonly TwitchEventSubClient _eventSubClient;
    private bool _disposed;

    public TwitchStreamingPlatformProvider(TwitchEventSubClient eventSubClient)
    {
        _eventSubClient = eventSubClient;
        _eventSubClient.EventReceivedAsync += ForwardTwitchEventAsync;
    }

    public StreamingPlatformDescriptor Descriptor => StreamingPlatformCatalog.Get(StreamingPlatform.Twitch);

    public bool IsRunning => _eventSubClient.IsRunning;

    public event Func<StreamEvent, CancellationToken, Task>? EventReceivedAsync;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        cancellationToken.ThrowIfCancellationRequested();
        return _eventSubClient.StartAsync();
    }

    public Task StopAsync() => _eventSubClient.StopAsync();

    public ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return ValueTask.CompletedTask;
        }

        _disposed = true;
        _eventSubClient.EventReceivedAsync -= ForwardTwitchEventAsync;
        return ValueTask.CompletedTask;
    }

    private async Task ForwardTwitchEventAsync(TwitchEvent twitchEvent, CancellationToken cancellationToken)
    {
        var handlers = EventReceivedAsync;
        if (handlers is null)
        {
            return;
        }

        var streamEvent = TwitchStreamEventAdapter.FromTwitch(twitchEvent);
        foreach (var handler in handlers.GetInvocationList().Cast<Func<StreamEvent, CancellationToken, Task>>())
        {
            await handler(streamEvent, cancellationToken);
        }
    }
}
