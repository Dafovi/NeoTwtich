using System.IO;
using System.Windows;
using System.Windows.Media;
using NeoTwitch.Models;
using NeoTwitch.Services.Text;

namespace NeoTwitch.Services;

public sealed class AudioPlayerService : IAsyncDisposable
{
    private readonly List<MediaPlayer> _players = [];
    private readonly IUiTextService _text;
    private int _disposed;

    public AudioPlayerService(IUiTextService text)
    {
        _text = text;
    }

    public async Task<TimeSpan?> ProbeDurationAsync(string? audioPath)
    {
        if (string.IsNullOrWhiteSpace(audioPath) || !File.Exists(audioPath))
        {
            return null;
        }

        var completion = new TaskCompletionSource<TimeSpan?>();

        _ = System.Windows.Application.Current.Dispatcher.BeginInvoke(() =>
        {
            var player = new MediaPlayer();

            void Cleanup()
            {
                player.MediaOpened -= Opened;
                player.MediaFailed -= Failed;
                player.Close();
            }

            void Opened(object? sender, EventArgs args)
            {
                var duration = player.NaturalDuration.HasTimeSpan
                    ? player.NaturalDuration.TimeSpan
                    : (TimeSpan?)null;

                completion.TrySetResult(duration);
                Cleanup();
            }

            void Failed(object? sender, ExceptionEventArgs args)
            {
                completion.TrySetResult(null);
                Cleanup();
            }

            player.MediaOpened += Opened;
            player.MediaFailed += Failed;
            player.Open(new Uri(audioPath, UriKind.Absolute));
        });

        try
        {
            return await completion.Task.WaitAsync(TimeSpan.FromSeconds(3));
        }
        catch (TimeoutException)
        {
            return null;
        }
    }

    public async Task<AudioPlayback?> PrepareAsync(string? audioPath, int volumePercent, Action<string> log)
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

        var completion = new TaskCompletionSource<AudioPlayback?>();

        _ = System.Windows.Application.Current.Dispatcher.BeginInvoke(() =>
        {
            var player = new MediaPlayer();
            _players.Add(player);

            void Cleanup()
            {
                player.MediaOpened -= Opened;
                player.MediaEnded -= Ended;
                player.MediaFailed -= Failed;
                player.Close();
                _players.Remove(player);
            }

            void Opened(object? sender, EventArgs args)
            {
                var duration = player.NaturalDuration.HasTimeSpan
                    ? player.NaturalDuration.TimeSpan
                    : (TimeSpan?)null;

                completion.TrySetResult(new AudioPlayback(player, duration, Cleanup));
            }

            void Ended(object? sender, EventArgs args)
            {
                Cleanup();
            }

            void Failed(object? sender, ExceptionEventArgs args)
            {
                log(_text.Format(UiTextKeys.AudioPlaybackFailureLog, args.ErrorException.Message));
                completion.TrySetResult(null);
                Cleanup();
            }

            player.MediaOpened += Opened;
            player.MediaEnded += Ended;
            player.MediaFailed += Failed;
            player.Open(new Uri(audioPath, UriKind.Absolute));
            player.Volume = Math.Clamp(volumePercent, ApplicationLimits.MinVolumePercent, ApplicationLimits.MaxVolumePercent) / 100d;
        });

        try
        {
            return await completion.Task.WaitAsync(TimeSpan.FromSeconds(3));
        }
        catch (TimeoutException)
        {
            log(_text.Get(UiTextKeys.AudioLoadTimeoutLog));
            return null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0 || _players.Count == 0)
        {
            return;
        }

        var application = System.Windows.Application.Current;
        if (application?.Dispatcher is null)
        {
            ClosePlayers();
            return;
        }

        await application.Dispatcher.InvokeAsync(ClosePlayers);
    }

    private void ClosePlayers()
    {
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
    private readonly MediaPlayer _player;
    private readonly Action _cleanup;
    private readonly TaskCompletionSource _completion = new();
    private bool _disposed;

    public AudioPlayback(MediaPlayer player, TimeSpan? duration, Action cleanup)
    {
        _player = player;
        _cleanup = cleanup;
        Duration = duration;
        _player.MediaEnded += Complete;
        _player.MediaFailed += CompleteFailed;
    }

    public TimeSpan? Duration { get; }

    public Task Completion => _completion.Task;

    public void Play()
    {
        _ = System.Windows.Application.Current.Dispatcher.BeginInvoke(() =>
        {
            if (!_disposed)
            {
                _player.Play();
            }
        });
    }

    public void Stop()
    {
        _ = System.Windows.Application.Current.Dispatcher.BeginInvoke(() =>
        {
            if (_disposed)
            {
                return;
            }

            _player.Stop();
            Dispose();
        });
    }

    private void Complete(object? sender, EventArgs args)
    {
        Dispose();
    }

    private void CompleteFailed(object? sender, ExceptionEventArgs args)
    {
        Dispose();
    }

    private void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _player.MediaEnded -= Complete;
        _player.MediaFailed -= CompleteFailed;
        _completion.TrySetResult();
        _cleanup();
    }
}
