using NeoTwitch.Models;
using NeoTwitch.ViewModels.Obs;

namespace NeoTwitch.Services.Alerts;

public static class AlertEffectWaitService
{
    public static async Task WaitAsync(
        AudioPlayback? playback,
        LightCommand? command,
        ObsMediaHideRequest? obsMediaHide,
        CancellationToken cancellationToken,
        TimeSpan? virtualLightDuration = null)
    {
        await WaitAsync(
            playback,
            command,
            obsMediaHide is null ? [] : [obsMediaHide],
            cancellationToken,
            virtualLightDuration);
    }

    public static async Task WaitAsync(
        AudioPlayback? playback,
        LightCommand? command,
        IReadOnlyCollection<ObsMediaHideRequest> obsMediaHides,
        CancellationToken cancellationToken,
        TimeSpan? virtualLightDuration = null)
    {
        var duration = AlertDurationService.ResolveMaxEffectDuration(
            playback?.Duration,
            command is null ? null : TimeSpan.FromMilliseconds(command.DurationMs),
            obsMediaHides.Count == 0 ? null : obsMediaHides.Max(media => media.Duration),
            virtualLightDuration);

        if (duration is { TotalMilliseconds: > 0 })
        {
            await Task.Delay(duration.Value, cancellationToken);
            return;
        }

        if (playback is not null)
        {
            await playback.Completion.WaitAsync(cancellationToken);
            return;
        }

        await Task.Delay(500, cancellationToken);
    }
}
