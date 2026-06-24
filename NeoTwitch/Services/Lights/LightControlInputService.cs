namespace NeoTwitch.Services.Lights;

public static class LightControlInputService
{
    public static RuleLightPresetValues GetRulePreset(string preset)
    {
        return preset switch
        {
            "Soft" => new RuleLightPresetValues(120, 4500, 160, 220),
            "Fast" => new RuleLightPresetValues(230, 2200, 35, 60),
            _ => new RuleLightPresetValues(180, 3500, 80, 120)
        };
    }

    public static BackgroundLightPresetValues GetBackgroundPreset(string preset)
    {
        return preset switch
        {
            "Soft" => new BackgroundLightPresetValues(110, 180, 260),
            "Fast" => new BackgroundLightPresetValues(220, 35, 70),
            _ => new BackgroundLightPresetValues(160, 90, 140)
        };
    }

    public static bool TryParseDelta(string? tag, out LightSliderDelta delta)
    {
        delta = new LightSliderDelta("", 0);
        var parts = (tag ?? "").Split(':', 2, StringSplitOptions.TrimEntries);
        if (parts.Length != 2 || !double.TryParse(parts[1], out var amount))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(parts[0]))
        {
            return false;
        }

        delta = new LightSliderDelta(parts[0], amount);
        return true;
    }

    public static double AdjustValue(double current, double delta, double minimum, double maximum)
    {
        return Math.Clamp(current + delta, minimum, maximum);
    }

    public static bool TryParseSliderText(string text, double minimum, double maximum, out double value)
    {
        value = 0;
        if (!double.TryParse(text.Trim(), out var parsed))
        {
            return false;
        }

        value = Math.Clamp(parsed, minimum, maximum);
        return true;
    }
}

public sealed record RuleLightPresetValues(double Brightness, double DurationMs, double CycleMs, double StepMs);

public sealed record BackgroundLightPresetValues(double Brightness, double CycleMs, double StepMs);

public sealed record LightSliderDelta(string Target, double Amount);
