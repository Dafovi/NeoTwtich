namespace NeoTwitch.Services;

public static class ArduinoReconnectBackoffPolicy
{
    private static readonly TimeSpan InitialDelay = TimeSpan.FromSeconds(8);
    private static readonly TimeSpan MaximumDelay = TimeSpan.FromMinutes(1);

    public static TimeSpan DelayAfterFailure(int consecutiveFailures)
    {
        if (consecutiveFailures <= 0)
        {
            return TimeSpan.Zero;
        }

        var multiplier = 1L << Math.Min(consecutiveFailures - 1, 6);
        var delay = TimeSpan.FromTicks(InitialDelay.Ticks * multiplier);
        return delay > MaximumDelay ? MaximumDelay : delay;
    }
}
