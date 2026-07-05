using NeoTwitch.Models;

namespace NeoTwitch.Services.Library;

public static class LibraryAssetUsageService
{
    public static void MarkAudioUsed(AudioAssetConfig audio, TimeSpan? duration, TimeProvider timeProvider)
    {
        if (duration is { TotalMilliseconds: > 0 })
        {
            audio.DurationMs = (int)Math.Round(duration.Value.TotalMilliseconds);
        }

        audio.LastUsedAt = timeProvider.GetUtcNow();
    }

    public static void MarkMediaUsed(MediaAssetConfig media, TimeProvider timeProvider)
    {
        media.LastUsedAt = timeProvider.GetUtcNow();
    }
}
