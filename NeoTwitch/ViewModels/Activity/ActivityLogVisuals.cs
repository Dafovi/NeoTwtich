using System.Windows.Media;
using System.Windows.Media.Imaging;

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
        return key switch
        {
            "Activity" => "M3,12 L7,12 L10,5 L14,19 L17,12 L21,12",
            "Arduino" => "M7,8 C4,8 2,10 2,12 C2,14 4,16 7,16 C9,16 10,14 12,12 C14,10 15,8 17,8 C20,8 22,10 22,12 C22,14 20,16 17,16 C15,16 14,14 12,12 C10,10 9,8 7,8 M5,12 L9,12 M17,10 L17,14 M15,12 L19,12",
            "Bits" => "M12,2 L20,9 L12,22 L4,9 Z M12,2 L12,22 M4,9 L20,9",
            "Chat" => "M4,5 L20,5 L20,16 L9,16 L5,20 L5,16 L4,16 Z M8,10 L16,10 M8,13 L13,13",
            "Event" => "M12,3 L14.5,9 L21,9 L15.5,13 L17.5,21 L12,16.5 L6.5,21 L8.5,13 L3,9 L9.5,9 Z",
            "Settings" => "M12,8 A4,4 0 1 1 12,16 A4,4 0 1 1 12,8 M12,2 L14,2 L15,5 L18,4 L20,6 L19,9 L22,11 L22,13 L19,15 L20,18 L18,20 L15,19 L14,22 L10,22 L9,19 L6,20 L4,18 L5,15 L2,13 L2,11 L5,9 L4,6 L6,4 L9,5 L10,2 Z",
            "Star" => "M12,3 L14.6,8.6 L20.8,9.3 L16.2,13.5 L17.5,19.8 L12,16.6 L6.5,19.8 L7.8,13.5 L3.2,9.3 L9.4,8.6 Z",
            "Sun" => "M12,7 A5,5 0 1 1 12,17 A5,5 0 1 1 12,7 M12,1 L12,4 M12,20 L12,23 M4.2,4.2 L6.3,6.3 M17.7,17.7 L19.8,19.8 M1,12 L4,12 M20,12 L23,12 M4.2,19.8 L6.3,17.7 M17.7,6.3 L19.8,4.2",
            "Users" => "M8,11 A4,4 0 1 1 8,3 A4,4 0 1 1 8,11 M2,21 C2,16 5,14 8,14 C11,14 14,16 14,21 M17,10 A3,3 0 1 1 17,4 A3,3 0 1 1 17,10 M15,14 C18,14 21,16 21,20",
            "Warning" => "M12,3 L22,20 L2,20 Z M12,8 L12,13 M12,17 L12.1,17",
            "Zap" => "M13,2 L4,14 L11,14 L9,22 L20,10 L13,10 Z",
            _ => "M12,5 L12,19 M5,12 L19,12"
        };
    }

    public static SolidColorBrush FrozenBrushFrom(string hex)
    {
        var brush = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(hex));
        brush.Freeze();
        return brush;
    }

    public static SolidColorBrush TranslucentBrushFrom(string accentColor)
    {
        return accentColor.StartsWith('#') && accentColor.Length == 7
            ? FrozenBrushFrom($"#22{accentColor[1..]}")
            : FrozenBrushFrom("#2200C7B7");
    }

    public static SolidColorBrush BackgroundBrushFrom(string accentColor)
    {
        return accentColor.StartsWith('#') && accentColor.Length == 7
            ? FrozenBrushFrom($"#22{accentColor[1..]}")
            : FrozenBrushFrom("#2200C7B7");
    }

    public static ImageSource? LoadIcon(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        foreach (var uri in new[]
        {
            $"pack://application:,,,/NeoTwitch;component/{path}",
            $"pack://application:,,,/{path}"
        })
        {
            try
            {
                var image = new BitmapImage(new Uri(uri, UriKind.Absolute));
                image.Freeze();
                return image;
            }
            catch
            {
                // Try the next pack URI format.
            }
        }

        return null;
    }
}
