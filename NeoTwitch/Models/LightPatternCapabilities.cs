namespace NeoTwitch.Models;

public static class LightPatternCapabilities
{
    public static bool UsesPrimaryColor(LightPattern pattern)
    {
        return pattern != LightPattern.Rainbow;
    }

    public static bool UsesSecondaryColor(LightPattern pattern)
    {
        return pattern is LightPattern.Pulse
            or LightPattern.Chase
            or LightPattern.Theater
            or LightPattern.Sparkle
            or LightPattern.Rave;
    }

    public static bool UsesTertiaryColor(LightPattern pattern)
    {
        return pattern is LightPattern.Chase
            or LightPattern.Theater
            or LightPattern.Sparkle
            or LightPattern.Rave;
    }

    public static bool UsesBrightness(LightPattern pattern)
    {
        return true;
    }

    public static bool UsesCycle(LightPattern pattern)
    {
        return pattern != LightPattern.Solid;
    }

    public static bool UsesStep(LightPattern pattern)
    {
        return pattern is LightPattern.Sparkle or LightPattern.Rave;
    }
}
