using NeoTwitch.Models;

namespace NeoTwitch.Services.Ui;

public static class UiAccentCatalog
{
    public const string Neutral = "#94A3B8";
    public const string AudioSingle = "#14B8A6";
    public const string AudioGroup = "#B56CFF";
    public const string ObsImage = "#37C7F3";
    public const string ObsVideo = "#B56CFF";
    public const string MediaSingle = "#14B8A6";
    public const string MediaGroup = "#22C55E";

    public static string ForEventKind(TwitchEventKind kind)
    {
        return kind switch
        {
            TwitchEventKind.Follow => "#14B8A6",
            TwitchEventKind.Subscription => "#B56CFF",
            TwitchEventKind.Raid => "#F43F5E",
            TwitchEventKind.Cheer => "#37C7F3",
            TwitchEventKind.ChatCommand => "#22C55E",
            TwitchEventKind.ChannelPointRedemption => "#FB923C",
            _ => Neutral
        };
    }

    public static string ForLightPattern(LightPattern pattern)
    {
        return pattern switch
        {
            LightPattern.Solid => "#14B8A6",
            LightPattern.Pulse => "#B56CFF",
            LightPattern.Rainbow => "#37C7F3",
            LightPattern.Chase => "#22C55E",
            LightPattern.Theater => "#F59E0B",
            LightPattern.Sparkle => "#FACC15",
            LightPattern.Rave => "#EC4899",
            _ => Neutral
        };
    }
}
