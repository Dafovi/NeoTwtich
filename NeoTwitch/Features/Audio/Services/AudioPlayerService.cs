using System.IO;
using NeoTwitch.Models;
using NeoTwitch.Services.Text;

namespace NeoTwitch.Services;

public sealed class AudioPlayerService : IAsyncDisposable
{
    private readonly List<IAudioMediaPlayer> _players = [];
    private readonly List<AudioPlayback> _activePlaybacks = [];
    private readonly IUiTextService _text;
    private readonly Func<IAudioMediaPlayer> _playerFactory;
    private readonly IAudioDispatcher _dispatcher;
    private readonly TimeSpan _loadTimeout;
    private int _disposed;

    public AudioPlayerService(IUiTextService text)
        : this(text, () => new WpfAudioMediaPlayer(), new WpfAudioDispatcher(), TimeSpan.FromSeconds(3)) { }

    internal AudioPlayerService(IUiTextService text, Func<IAudioMediaPlayer> playerFactory,
        IAudioDispatcher dispatcher, TimeSpan loadTimeout)
    {
        _text = text;
        _playerFactory = playerFactory;
        _dispatcher = dispatcher;
        _loadTimeout = loadTimeout;
    }

    internal int TrackedPlayerCount => _players.Count;

    public Task<TimeSpan?> ProbeDurationAsync(string? audioPath) =>
        ProbeDurationAsync(audioPath, CancellationToken.None);

    internal async Task<TimeSpan?> ProbeDurationAsync(string? audioPath, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(audioPath) || !File.Exists(audioPath)) return null;

        var completion = new TaskCompletionSource<TimeSpan?>(TaskCreationOptions.RunContinuationsAsynchronously);
        IAudioMediaPlayer? player = null;
        EventHandler? opened = null;
        EventHandler<AudioMediaFailedEventArgs>? failed = null;
        var cleaned = 0;

        void Cleanup()
        {
            if (Interlocked.Exchange(ref cleaned, 1) != 0 || player is null) return;
            player.Opened -= opened;
            player.Failed -= failed;
            player.Stop();
            player.Close();
        }

        await _dispatcher.InvokeAsync(() =>
        {
            player = _playerFactory();
            opened = (_, _) => completion.TrySetResult(player.Duration);
            failed = (_, _) => completion.TrySetResult(null);
            player.Opened += opened;
            player.Failed += failed;
            player.Open(new Uri(audioPath, UriKind.Absolute));
        });

        try
        {
            return await completion.Task.WaitAsync(_loadTimeout, cancellationToken);
        }
        catch (TimeoutException)
        {
            return null;
        }
        finally
        {
            await _dispatcher.InvokeAsync(Cleanup);
        }
    }

    public async Task<AudioPlayback?> PrepareAsync(string? audioPath, int volumePercent, Action<string> log,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(audioPath))
        {
            log(_text.Get(UiTextKeys.AudioRuleMissingAudioLog));
            return null;
        }

        if (!File.Exists(audioPath))
        {
            log(_text.Format(UiTextKeys.AudioFileMissingLog, audioPath));
            return null;
        }

        var completion = new TaskCompletionSource<AudioPlayback?>(TaskCreationOptions.RunContinuationsAsynchronously);
        IAudioMediaPlayer? player = null;
        EventHandler? opened = null;
        EventHandler<AudioMediaFailedEventArgs>? failed = null;
        var cleaned = 0;
        var ownershipTransferred = false;
        AudioPlayback? transferredPlayback = null;

        void Cleanup()
        {
            if (Interlocked.Exchange(ref cleaned, 1) != 0 || player is null) return;
            player.Opened -= opened;
            player.Failed -= failed;
            player.Stop();
            player.Close();
            _players.Remove(player);
            if (transferredPlayback is not null) _activePlaybacks.Remove(transferredPlayback);
        }

        await _dispatcher.InvokeAsync(() =>
        {
            player = _playerFactory();
            _players.Add(player);
            opened = (_, _) =>
            {
                var playback = new AudioPlayback(player, player.Duration, Cleanup, _dispatcher);
                transferredPlayback = playback;
                _activePlaybacks.Add(playback);
                if (completion.TrySetResult(playback)) ownershipTransferred = true;
                else playback.Release();
            };
            failed = (_, args) =>
            {
                log(_text.Format(UiTextKeys.AudioPlaybackFailureLog, args.Exception.Message));
                completion.TrySetResult(null);
            };
            player.Opened += opened;
            player.Failed += failed;
            player.Open(new Uri(audioPath, UriKind.Absolute));
            player.Volume = Math.Clamp(volumePercent, ApplicationLimits.MinVolumePercent,
                ApplicationLimits.MaxVolumePercent) / 100d;
        });

        try
        {
            var playback = await completion.Task.WaitAsync(_loadTimeout, cancellationToken);
            if (playback is null) await _dispatcher.InvokeAsync(Cleanup);
            return playback;
        }
        catch (TimeoutException)
        {
            log(_text.Get(UiTextKeys.AudioLoadTimeoutLog));
            return null;
        }
        finally
        {
            if (!ownershipTransferred) await _dispatcher.InvokeAsync(Cleanup);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
        await _dispatcher.InvokeAsync(ClosePlayers);
    }

    private void ClosePlayers()
    {
        foreach (var playback in _activePlaybacks.ToArray()) playback.Release();
        foreach (var player in _players.ToArray())
        {
            player.Stop();
            player.Close();
        }
        _players.Clear();
    }
}

public sealed class AudioPlayback
{
    private readonly IAudioMediaPlayer _player;
    private readonly Action _cleanup;
    private readonly IAudioDispatcher _dispatcher;
    private readonly TaskCompletionSource _completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private bool _disposed;

    internal AudioPlayback(IAudioMediaPlayer player, TimeSpan? duration, Action cleanup, IAudioDispatcher dispatcher)
    {
        _player = player;
        _cleanup = cleanup;
        _dispatcher = dispatcher;
        Duration = duration;
        _player.Ended += Complete;
        _player.Failed += CompleteFailed;
    }

    public TimeSpan? Duration { get; }
    public Task Completion => _completion.Task;
    public void Play() => _dispatcher.Post(() => { if (!_disposed) _player.Play(); });
    public void Stop() => _dispatcher.Post(() => { if (!_disposed) { _player.Stop(); Release(); } });
    private void Complete(object? sender, EventArgs args) => Release();
    private void CompleteFailed(object? sender, AudioMediaFailedEventArgs args) => Release();

    internal void Release()
    {
        if (_disposed) return;
        _disposed = true;
        _player.Ended -= Complete;
        _player.Failed -= CompleteFailed;
        _completion.TrySetResult();
        _cleanup();
    }
}
