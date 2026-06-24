using System.IO;
using System.Windows;
using System.Windows.Media;
using NeoTwitch.Models;

namespace NeoTwitch.Services;

public sealed class AudioPlayerService
{
    private readonly List<MediaPlayer> _players = [];

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
            log("La regla no tiene audio configurado.");
            return null;
        }

        if (!File.Exists(audioPath))
        {
            log($"No encontre el audio: {audioPath}");
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
                log($"No se pudo reproducir el audio: {args.ErrorException.Message}");
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
            log("El audio tardo demasiado en cargar; usare la duracion manual de la regla.");
            return null;
        }
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
