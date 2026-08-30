using System.Windows.Media;

namespace NeoTwitch.Services;

internal interface IAudioMediaPlayer
{
    event EventHandler? Opened;
    event EventHandler? Ended;
    event EventHandler<AudioMediaFailedEventArgs>? Failed;
    TimeSpan? Duration { get; }
    double Volume { set; }
    void Open(Uri uri);
    void Play();
    void Stop();
    void Close();
}

internal sealed class AudioMediaFailedEventArgs(Exception exception) : EventArgs
{
    public Exception Exception { get; } = exception;
}

internal sealed class WpfAudioMediaPlayer : IAudioMediaPlayer
{
    private readonly MediaPlayer _player = new();

    public WpfAudioMediaPlayer()
    {
        _player.MediaOpened += (_, _) => Opened?.Invoke(this, EventArgs.Empty);
        _player.MediaEnded += (_, _) => Ended?.Invoke(this, EventArgs.Empty);
        _player.MediaFailed += (_, args) => Failed?.Invoke(this, new AudioMediaFailedEventArgs(args.ErrorException));
    }

    public event EventHandler? Opened;
    public event EventHandler? Ended;
    public event EventHandler<AudioMediaFailedEventArgs>? Failed;
    public TimeSpan? Duration => _player.NaturalDuration.HasTimeSpan ? _player.NaturalDuration.TimeSpan : null;
    public double Volume { set => _player.Volume = value; }
    public void Open(Uri uri) => _player.Open(uri);
    public void Play() => _player.Play();
    public void Stop() => _player.Stop();
    public void Close() => _player.Close();
}

internal interface IAudioDispatcher
{
    void Post(Action action);
    Task InvokeAsync(Action action);
}

internal sealed class WpfAudioDispatcher : IAudioDispatcher
{
    public void Post(Action action)
    {
        var dispatcher = System.Windows.Application.Current?.Dispatcher;
        if (dispatcher is null) action();
        else _ = dispatcher.BeginInvoke(action);
    }

    public async Task InvokeAsync(Action action)
    {
        var dispatcher = System.Windows.Application.Current?.Dispatcher;
        if (dispatcher is null) action();
        else await dispatcher.InvokeAsync(action);
    }
}
