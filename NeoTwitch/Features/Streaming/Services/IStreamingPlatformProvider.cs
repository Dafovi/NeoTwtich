using NeoTwitch.Models;

namespace NeoTwitch.Services.Streaming;

/// <summary>
/// Contract implemented by each streaming service. Authentication details stay inside each provider.
/// </summary>
public interface IStreamingPlatformProvider : IAsyncDisposable
{
    StreamingPlatformDescriptor Descriptor { get; }

    bool IsRunning { get; }

    event Func<StreamEvent, CancellationToken, Task>? EventReceivedAsync;

    Task StartAsync(CancellationToken cancellationToken);

    Task StopAsync();
}
