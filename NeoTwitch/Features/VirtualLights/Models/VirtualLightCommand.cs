namespace NeoTwitch.Models;

public sealed record VirtualLightCommand(
    LightPattern Pattern,
    string PrimaryColor,
    string SecondaryColor,
    string TertiaryColor,
    int Brightness,
    int DurationMs,
    int CycleMs,
    int StepMs,
    int ObsOpacity,
    int ScreenPixelSize,
    int ScreenSaturation)
{
    public static VirtualLightCommand FromRule(EventRule rule, int? durationOverrideMs = null)
    {
        return FromSnapshot(AlertExecutionSnapshotFactory.Create(rule), durationOverrideMs);
    }

    public static VirtualLightCommand FromSnapshot(
        AlertExecutionRuleSnapshot rule,
        int? durationOverrideMs = null)
    {
        var lights = rule.VirtualLights;
        return new VirtualLightCommand(
            lights.Pattern,
            LightCommand.NormalizeColor(lights.PrimaryColor),
            LightCommand.NormalizeColor(lights.SecondaryColor),
            LightCommand.NormalizeColor(lights.TertiaryColor),
            Math.Clamp(lights.Brightness, ApplicationLimits.MinBrightness, ApplicationLimits.MaxBrightness),
            Math.Clamp(durationOverrideMs ?? lights.DurationMs, ApplicationLimits.MinAlertDurationMs, ApplicationLimits.MaxAlertDurationMs),
            Math.Clamp(lights.CycleMs, ApplicationLimits.MinCycleMs, ApplicationLimits.MaxCycleMs),
            Math.Clamp(lights.StepMs, ApplicationLimits.MinStepMs, ApplicationLimits.MaxStepMs),
            Math.Clamp(lights.ObsOpacity, 0, 100),
            Math.Clamp(lights.ScreenPixelSize, 4, 80),
            Math.Clamp(lights.ScreenSaturation, 0, 200));
    }
}
