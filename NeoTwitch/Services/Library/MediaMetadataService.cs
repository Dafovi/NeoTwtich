using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using NeoTwitch.Models;

namespace NeoTwitch.Services.Library;

public static class MediaMetadataService
{
    public static string BuildVideoMetadata(MediaAssetConfig asset)
    {
        if (asset.DurationMs > 0)
        {
            return asset.DurationText;
        }

        var extension = Path.GetExtension(asset.FilePath);
        return string.IsNullOrWhiteSpace(extension)
            ? "Video"
            : extension.TrimStart('.').ToUpperInvariant();
    }

    public static (int Width, int Height) ProbeImageSize(string path)
    {
        try
        {
            var frame = BitmapFrame.Create(new Uri(path), BitmapCreateOptions.DelayCreation, BitmapCacheOption.None);
            return (frame.PixelWidth, frame.PixelHeight);
        }
        catch
        {
            return (0, 0);
        }
    }

    public static int ProbeVideoDurationMs(string path)
    {
        if (!File.Exists(path))
        {
            return 0;
        }

        var durationMs = 0;

        try
        {
            var dispatcher = Dispatcher.CurrentDispatcher;
            var frame = new DispatcherFrame();
            var player = new MediaPlayer();
            var timeout = new DispatcherTimer(DispatcherPriority.Background, dispatcher)
            {
                Interval = TimeSpan.FromSeconds(2)
            };

            void Finish()
            {
                timeout.Stop();
                frame.Continue = false;
            }

            player.MediaOpened += (_, _) =>
            {
                if (player.NaturalDuration.HasTimeSpan)
                {
                    durationMs = (int)Math.Clamp(
                        Math.Round(player.NaturalDuration.TimeSpan.TotalMilliseconds),
                        0,
                        ApplicationLimits.MaxMediaDurationMs);
                }

                Finish();
            };
            player.MediaFailed += (_, _) => Finish();
            timeout.Tick += (_, _) => Finish();

            timeout.Start();
            player.Open(new Uri(path, UriKind.Absolute));
            Dispatcher.PushFrame(frame);
            player.Close();
        }
        catch
        {
            return 0;
        }

        return durationMs;
    }
}
