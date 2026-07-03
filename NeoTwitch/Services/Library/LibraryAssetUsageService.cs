using NeoTwitch.Models;

namespace NeoTwitch.Services.Library;

public static class LibraryAssetUsageService
{
    public static void MarkAudioUsed(AudioAssetConfig audio, TimeSpan? duration, DateTimeOffset? usedAt = null)
    {
        if (duration is { TotalMilliseconds: > 0 })
        {
            audio.DurationMs = (int)Math.Round(duration.Value.TotalMilliseconds);
        }

        audio.LastUsedAt = usedAt ?? DateTimeOffset.Now;
    }

    public static void MarkMediaUsed(MediaAssetConfig media, DateTimeOffset? usedAt = null)
    {
        media.LastUsedAt = usedAt ?? DateTimeOffset.Now;
    }
}
