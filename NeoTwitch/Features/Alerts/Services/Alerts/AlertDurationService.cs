using NeoTwitch.Models;

namespace NeoTwitch.Services.Alerts;

public static class AlertDurationService
{
    public static int? ResolveSynchronizedEffectDurationMs(params TimeSpan?[] durations)
    {
        var maxDuration = ResolveMaxEffectDuration(durations);
        return maxDuration is { TotalMilliseconds: > 0 }
            ? Math.Clamp((int)Math.Round(maxDuration.Value.TotalMilliseconds), ApplicationLimits.MinAlertDurationMs, ApplicationLimits.MaxAlertDurationMs)
            : null;
    }

    public static TimeSpan? ResolveMaxEffectDuration(params TimeSpan?[] durations)
    {
        TimeSpan? maxDuration = null;
        foreach (var duration in durations)
        {
            if (duration is not { TotalMilliseconds: > 0 })
            {
                continue;
            }

            if (maxDuration is null || duration.Value > maxDuration.Value)
            {
                maxDuration = duration.Value;
            }
        }

        return maxDuration;
    }
}
