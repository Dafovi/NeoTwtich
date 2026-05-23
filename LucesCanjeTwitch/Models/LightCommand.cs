namespace LucesCanjeTwitch.Models;

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
    public static LightCommand FromRule(EventRule rule, AppConfig config, int? durationOverrideMs = null)
    {
        return new LightCommand(
            ResolveTargets(config, rule.TargetPins),
            rule.Pattern,
            NormalizeColor(rule.PrimaryColor),
            NormalizeColor(rule.SecondaryColor),
            NormalizeColor(rule.TertiaryColor),
            Math.Clamp(rule.Brightness, 0, 255),
            Math.Clamp(durationOverrideMs ?? rule.DurationMs, 250, 600000),
            Math.Clamp(rule.CycleMs, 10, 2000),
            Math.Clamp(rule.StepMs, 10, 5000));
    }

    public static LightCommand FromBackground(AppConfig config)
    {
        return new LightCommand(
            ResolveTargets(config, config.BackgroundTargetPins),
            config.BackgroundPattern,
            NormalizeColor(config.BackgroundPrimaryColor),
            NormalizeColor(config.BackgroundSecondaryColor),
            NormalizeColor(config.BackgroundTertiaryColor),
            Math.Clamp(config.BackgroundBrightness, 0, 255),
            0,
            Math.Clamp(config.BackgroundCycleMs, 10, 2000),
            Math.Clamp(config.BackgroundStepMs, 10, 5000));
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
        var strips = config.LedStrips.Count == 0
            ? AppConfig.CreateDefault().LedStrips
            : config.LedStrips;

        var targets = strips
            .Where(strip => selectedPins.Count == 0 || selectedPins.Contains(strip.Pin))
            .GroupBy(strip => strip.Pin)
            .Select(group => group.First())
            .Select(strip => new LightStripTarget(strip.Pin, strip.LedCount))
            .ToArray();

        return targets.Length > 0
            ? targets
            : strips.Select(strip => new LightStripTarget(strip.Pin, strip.LedCount)).ToArray();
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
            .Where(pin => pin is >= 0 and <= 53)
            .Select(pin => pin!.Value)
            .Distinct()
            .ToArray();
    }

    private static string FormatTargets(IReadOnlyList<LightStripTarget> targets)
    {
        return string.Join(',', targets.Select(target => target.ToProtocolValue()));
    }
}
