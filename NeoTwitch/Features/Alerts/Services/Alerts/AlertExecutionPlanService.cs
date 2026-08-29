using NeoTwitch.Models;

namespace NeoTwitch.Services.Alerts;

public sealed record AlertExecutionPlan(
    bool UseLights,
    bool ShouldReconnectArduino,
    bool ShouldRestoreBackground,
    IReadOnlyList<LightStripTarget> AllLightTargets,
    IReadOnlyList<LightStripTarget> RuleLightTargets,
    int? SynchronizedDurationMs,
    LightCommand? LightCommand);

public static class AlertExecutionPlanService
{
    public static AlertExecutionPlan Build(
        EventRule rule,
        AppConfig config,
        bool hasOpenArduinoPort,
        TimeSpan? playbackDuration,
        TimeSpan? obsMediaDuration)
    {
        return Build(AlertExecutionSnapshotFactory.Create(rule), config, hasOpenArduinoPort, playbackDuration, obsMediaDuration);
    }

    public static AlertExecutionPlan Build(
        AlertExecutionRuleSnapshot rule,
        AppConfig config,
        bool hasOpenArduinoPort,
        TimeSpan? playbackDuration,
        TimeSpan? obsMediaDuration)
    {
        var useLights = config.ArduinoEnabled && rule.Lights.Enabled;
        if (!useLights)
        {
            return new AlertExecutionPlan(
                UseLights: false,
                ShouldReconnectArduino: false,
                ShouldRestoreBackground: false,
                AllLightTargets: [],
                RuleLightTargets: [],
                SynchronizedDurationMs: AlertDurationService.ResolveSynchronizedEffectDurationMs(playbackDuration, obsMediaDuration),
                LightCommand: null);
        }

        var syncedDurationMs = AlertDurationService.ResolveSynchronizedEffectDurationMs(playbackDuration, obsMediaDuration);
        return new AlertExecutionPlan(
            UseLights: true,
            ShouldReconnectArduino: !hasOpenArduinoPort && !string.IsNullOrWhiteSpace(config.SerialPort),
            ShouldRestoreBackground: true,
            AllLightTargets: LightCommand.ResolveTargets(config, ""),
            RuleLightTargets: LightCommand.ResolveTargets(config, rule.Lights.TargetPins),
            SynchronizedDurationMs: syncedDurationMs,
            LightCommand: LightCommand.FromSnapshot(rule, config, syncedDurationMs));
    }
}
