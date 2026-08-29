namespace NeoTwitch.Models;

public sealed record LightStripTarget(int Pin, int LedCount)
{
    public string ToProtocolValue() => $"{Pin}:{LedCount}";
}

public sealed record LightCommand(
    IReadOnlyList<LightStripTarget> Targets,
    LightPattern Pattern,
    string PrimaryColor,
    string SecondaryColor,
    string TertiaryColor,
    int Brightness,
    int DurationMs,
    int CycleMs,
    int StepMs)
{
    private const int DefaultTargetPin = 6;
    private const int DefaultTargetLedCount = 30;

    public static LightCommand FromRule(EventRule rule, AppConfig config, int? durationOverrideMs = null)
    {
        return FromSnapshot(AlertExecutionSnapshotFactory.Create(rule), config, durationOverrideMs);
    }

    public static LightCommand FromSnapshot(
        AlertExecutionRuleSnapshot rule,
        AppConfig config,
        int? durationOverrideMs = null)
    {
        var lights = rule.Lights;
        return new LightCommand(
            ResolveTargets(config, lights.TargetPins),
            lights.Pattern,
            NormalizeColor(lights.PrimaryColor),
            NormalizeColor(lights.SecondaryColor),
            NormalizeColor(lights.TertiaryColor),
            Math.Clamp(lights.Brightness, ApplicationLimits.MinBrightness, ApplicationLimits.MaxBrightness),
            Math.Clamp(durationOverrideMs ?? lights.DurationMs, ApplicationLimits.MinAlertDurationMs, ApplicationLimits.MaxAlertDurationMs),
            Math.Clamp(lights.CycleMs, ApplicationLimits.MinCycleMs, ApplicationLimits.MaxCycleMs),
            Math.Clamp(lights.StepMs, ApplicationLimits.MinStepMs, ApplicationLimits.MaxStepMs));
    }

    public static LightCommand FromBackground(AppConfig config)
    {
        return new LightCommand(
            ResolveTargets(config, config.BackgroundTargetPins),
            config.BackgroundPattern,
            NormalizeColor(config.BackgroundPrimaryColor),
            NormalizeColor(config.BackgroundSecondaryColor),
            NormalizeColor(config.BackgroundTertiaryColor),
            Math.Clamp(config.BackgroundBrightness, ApplicationLimits.MinBrightness, ApplicationLimits.MaxBrightness),
            0,
            Math.Clamp(config.BackgroundCycleMs, ApplicationLimits.MinCycleMs, ApplicationLimits.MaxCycleMs),
            Math.Clamp(config.BackgroundStepMs, ApplicationLimits.MinStepMs, ApplicationLimits.MaxStepMs));
    }

    public string ToProtocolLine()
    {
        var pattern = Pattern.ToString().ToUpperInvariant();
        var targets = FormatTargets(Targets);
        return $"FX|{targets}|{pattern}|{Brightness}|{DurationMs}|{CycleMs}|{StepMs}|{NormalizeColor(PrimaryColor)}|{NormalizeColor(SecondaryColor)}|{NormalizeColor(TertiaryColor)}\n";
    }

    public static string ToStopProtocolLine(IReadOnlyList<LightStripTarget> targets)
    {
        return $"STOP|{FormatTargets(targets)}\n";
    }

    public static IReadOnlyList<LightStripTarget> ResolveTargets(AppConfig config, string? targetPins)
    {
        var selectedPins = ParsePins(targetPins ?? "");
        if (config.LedStrips.Count == 0)
        {
            return [new LightStripTarget(DefaultTargetPin, DefaultTargetLedCount)];
        }

        var targets = config.LedStrips
            .Where(strip => selectedPins.Count == 0 || selectedPins.Contains(strip.Pin))
            .GroupBy(strip => strip.Pin)
            .Select(group => group.First())
            .Select(strip => new LightStripTarget(strip.Pin, strip.LedCount))
            .ToArray();

        return targets.Length > 0
            ? targets
            : config.LedStrips.Select(strip => new LightStripTarget(strip.Pin, strip.LedCount)).ToArray();
    }

    public static string NormalizeColor(string? color)
    {
        if (string.IsNullOrWhiteSpace(color))
        {
            return "#FFFFFF";
        }

        var value = color.Trim();
        if (!value.StartsWith('#'))
        {
            value = $"#{value}";
        }

        return value.Length == 7 && value.Skip(1).All(Uri.IsHexDigit)
            ? value.ToUpperInvariant()
            : "#FFFFFF";
    }

    public static IReadOnlyList<int> ParsePins(string text)
    {
        return text.Split([',', ';', ' ', '\r', '\n', '\t'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(value => int.TryParse(value, out var pin) ? pin : (int?)null)
            .Where(pin => pin is >= ApplicationLimits.MinArduinoPin and <= ApplicationLimits.MaxArduinoPin)
            .Select(pin => pin!.Value)
            .Distinct()
            .ToArray();
    }

    private static string FormatTargets(IReadOnlyList<LightStripTarget> targets)
    {
        return string.Join(',', targets.Select(target => target.ToProtocolValue()));
    }
}
