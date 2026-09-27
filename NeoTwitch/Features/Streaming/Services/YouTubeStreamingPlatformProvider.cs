using NeoTwitch.Models;
using NeoTwitch.Services.YouTube;

namespace NeoTwitch.Services.Streaming;

/// <summary>
/// Polls the active YouTube live chat with the interval instructed by YouTube and forwards only
/// messages observed after the initial history page.
/// </summary>
public sealed class YouTubeStreamingPlatformProvider : IStreamingPlatformProvider
{
    private static readonly TimeSpan OfflineRetryDelay = TimeSpan.FromSeconds(15);
    private static readonly TimeSpan FailureRetryDelay = TimeSpan.FromSeconds(10);
    private readonly object _sync = new();
    private readonly Func<AppConfig> _getConfig;
    private readonly YouTubeAuthService _authService;
    private readonly YouTubeLiveService _liveService;
    private readonly YouTubeLiveChatService _chatService;
    private readonly Action _saveConfig;
    private readonly Action<string> _log;
    private readonly TimeProvider _timeProvider;
    private CancellationTokenSource? _cancellation;
    private Task? _loop;
    private bool _disposed;

    public YouTubeStreamingPlatformProvider(
        Func<AppConfig> getConfig,
        YouTubeAuthService authService,
        YouTubeLiveService liveService,
        YouTubeLiveChatService chatService,
        Action saveConfig,
        Action<string> log,
        TimeProvider timeProvider)
    {
        _getConfig = getConfig;
        _authService = authService;
        _liveService = liveService;
        _chatService = chatService;
        _saveConfig = saveConfig;
        _log = log;
        _timeProvider = timeProvider;
    }

    public StreamingPlatformDescriptor Descriptor => StreamingPlatformCatalog.Get(StreamingPlatform.YouTube);

    public bool IsRunning => _loop is { IsCompleted: false };

    public event Func<StreamEvent, CancellationToken, Task>? EventReceivedAsync;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!_getConfig().YouTubeToken.HasToken)
        {
            throw new InvalidOperationException("Conecta una cuenta de YouTube antes de iniciar el chat en vivo.");
        }

        lock (_sync)
        {
            if (IsRunning)
            {
                return Task.CompletedTask;
            }

            _cancellation?.Dispose();
            _cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _loop = RunAsync(_cancellation.Token);
        }

        return Task.CompletedTask;
    }

    public async Task StopAsync()
    {
        Task? loop;
        lock (_sync)
        {
            _cancellation?.Cancel();
            loop = _loop;
        }

        if (loop is not null)
        {
            try
            {
                await loop;
            }
            catch (OperationCanceledException)
            {
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        await StopAsync();
        _cancellation?.Dispose();
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        var announcedLiveChatId = "";
        var pageToken = "";
        var isPrimed = false;
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var config = _getConfig();
                var accessToken = config.YouTubeToken.AccessToken;
                await _authService.EnsureValidTokenAsync(config, cancellationToken);
                if (!string.Equals(accessToken, config.YouTubeToken.AccessToken, StringComparison.Ordinal))
                {
                    _saveConfig();
                }

                var broadcast = await _liveService.GetActiveBroadcastAsync(config.YouTubeToken, cancellationToken);
                if (!broadcast.IsLive || string.IsNullOrWhiteSpace(broadcast.LiveChatId))
                {
                    announcedLiveChatId = "";
                    pageToken = "";
                    isPrimed = false;
                    await Task.Delay(OfflineRetryDelay, cancellationToken);
                    continue;
                }

                if (!string.Equals(announcedLiveChatId, broadcast.LiveChatId, StringComparison.Ordinal))
                {
                    announcedLiveChatId = broadcast.LiveChatId;
                    pageToken = "";
                    isPrimed = false;
                    _log($"YouTube: escuchando el chat de '{broadcast.Title}'.");
                }

                var page = await _chatService.GetMessagesAsync(config.YouTubeToken, broadcast.LiveChatId, pageToken, cancellationToken);
                pageToken = page.NextPageToken;
                if (isPrimed)
                {
                    foreach (var message in page.Messages)
                    {
                        var streamEvent = YouTubeStreamEventAdapter.FromLiveChatMessage(message);
                        if (streamEvent is not null)
                        {
                            await PublishAsync(streamEvent, cancellationToken);
                        }
                    }
                }
                else
                {
                    isPrimed = true;
                }

                if (page.IsOffline)
                {
                    announcedLiveChatId = "";
                    pageToken = "";
                    isPrimed = false;
                    await Task.Delay(OfflineRetryDelay, cancellationToken);
                }
                else
                {
                    await Task.Delay(page.PollingInterval, cancellationToken);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _log($"YouTube chat: {ex.Message}. Reintentando.");
                await Task.Delay(FailureRetryDelay, cancellationToken);
            }
        }
    }

    private async Task PublishAsync(StreamEvent streamEvent, CancellationToken cancellationToken)
    {
        var handlers = EventReceivedAsync;
        if (handlers is null)
        {
            return;
        }

        foreach (var handler in handlers.GetInvocationList().Cast<Func<StreamEvent, CancellationToken, Task>>())
        {
            await handler(streamEvent, cancellationToken);
        }
    }
}
