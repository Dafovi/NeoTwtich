using System.Windows.Media;
using NeoTwitch.Services.Ui;

namespace NeoTwitch.ViewModels.Activity;

internal static class ActivityLogVisuals
{
    public static string FilterAccent(string filter)
    {
        return filter.ToUpperInvariant() switch
        {
            "TWITCH" => "#9146FF",
            "ARDUINO" => "#00878F",
            "ALEXA" => "#2FB4E9",
            "AUDIO" => "#B56CFF",
            "OBS" => "#22C55E",
            "EVENTO" => "#22C55E",
            "SISTEMA" => "#94A3B8",
            "IMPORTANTE" => "#FFB020",
            _ => "#14B8A6"
        };
    }

    public static string IconData(string key)
    {
        return IconPathCatalog.Get(key);
    }

    public static SolidColorBrush FrozenBrushFrom(string hex) => UiBrushFactory.FrozenBrushFrom(hex);

    public static SolidColorBrush TranslucentBrushFrom(string accentColor) => UiBrushFactory.TranslucentBrushFrom(accentColor);

    public static SolidColorBrush BackgroundBrushFrom(string accentColor) => UiBrushFactory.TranslucentBrushFrom(accentColor);

    public static ImageSource? LoadIcon(string path)
    {
        return string.IsNullOrWhiteSpace(path)
            ? null
            : PackImageLoader.Load(path);
    }
}
