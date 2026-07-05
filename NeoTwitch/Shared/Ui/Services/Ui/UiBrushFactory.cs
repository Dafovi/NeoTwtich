using System.Windows.Media;

namespace NeoTwitch.Services.Ui;

public static class UiBrushFactory
{
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
}
