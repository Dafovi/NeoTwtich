using NeoTwitch.Models;

namespace NeoTwitch.Services.Alerts;

public sealed record AlertQueueOptions(
    int MaxQueuedSameRuleAlerts,
    int SameRuleQueueCooldownMs,
    int MaxQueuedDifferentRuleAlerts,
    int DifferentRuleQueueCooldownMs)
{
    public static AlertQueueOptions FromConfig(AppConfig config)
    {
        return new AlertQueueOptions(
            Math.Clamp(config.MaxQueuedSameRuleAlerts, 0, 100),
            Math.Clamp(config.SameRuleQueueCooldownMs, 0, ApplicationLimits.MaxAlertDurationMs),
            Math.Clamp(config.MaxQueuedDifferentRuleAlerts, 0, 100),
            Math.Clamp(config.DifferentRuleQueueCooldownMs, 0, ApplicationLimits.MaxAlertDurationMs));
    }
}
