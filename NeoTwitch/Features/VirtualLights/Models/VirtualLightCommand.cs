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
        return new VirtualLightCommand(
            rule.VirtualLightsPattern,
            LightCommand.NormalizeColor(rule.VirtualLightsPrimaryColor),
            LightCommand.NormalizeColor(rule.VirtualLightsSecondaryColor),
            LightCommand.NormalizeColor(rule.VirtualLightsTertiaryColor),
            Math.Clamp(rule.VirtualLightsBrightness, ApplicationLimits.MinBrightness, ApplicationLimits.MaxBrightness),
            Math.Clamp(durationOverrideMs ?? rule.VirtualLightsDurationMs, ApplicationLimits.MinAlertDurationMs, ApplicationLimits.MaxAlertDurationMs),
            Math.Clamp(rule.VirtualLightsCycleMs, ApplicationLimits.MinCycleMs, ApplicationLimits.MaxCycleMs),
            Math.Clamp(rule.VirtualLightsStepMs, ApplicationLimits.MinStepMs, ApplicationLimits.MaxStepMs),
            Math.Clamp(rule.VirtualLightsObsOpacity, 0, 100),
            Math.Clamp(rule.VirtualLightsScreenPixelSize, 4, 80),
            Math.Clamp(rule.VirtualLightsScreenSaturation, 0, 200));
    }
}
