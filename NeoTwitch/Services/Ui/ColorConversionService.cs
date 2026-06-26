using System.Globalization;
using System.Windows.Media;
using WpfColor = System.Windows.Media.Color;
using WpfColorConverter = System.Windows.Media.ColorConverter;

namespace NeoTwitch.Services.Ui;

public readonly record struct HsvColor(double Hue, double Saturation, double Value);

public static class ColorConversionService
{
    public static WpfColor ParseColor(string? value, WpfColor fallback)
    {
        try
        {
            var text = NormalizeHex(value);
            return string.IsNullOrWhiteSpace(text)
                ? fallback
                : (WpfColor)WpfColorConverter.ConvertFromString(text)!;
        }
        catch
        {
            return fallback;
        }
    }

    public static string NormalizeHex(string? value)
    {
        var text = string.IsNullOrWhiteSpace(value) ? "" : value.Trim();
        return string.IsNullOrWhiteSpace(text)
            ? ""
            : text.StartsWith('#')
                ? text
                : $"#{text}";
    }

    public static string ToHex(WpfColor color)
    {
        return string.Create(CultureInfo.InvariantCulture, $"#{color.R:X2}{color.G:X2}{color.B:X2}");
    }

    public static WpfColor FromHsv(double hue, double saturation, double value)
    {
        var normalizedHue = ((hue % 360) + 360) % 360;
        var chroma = value * saturation;
        var huePrime = normalizedHue / 60.0;
        var x = chroma * (1 - Math.Abs(huePrime % 2 - 1));

        var (r1, g1, b1) = huePrime switch
        {
            < 1 => (chroma, x, 0d),
            < 2 => (x, chroma, 0d),
            < 3 => (0d, chroma, x),
            < 4 => (0d, x, chroma),
            < 5 => (x, 0d, chroma),
            _ => (chroma, 0d, x)
        };

        var match = value - chroma;
        return WpfColor.FromRgb(
            (byte)Math.Round((r1 + match) * 255),
            (byte)Math.Round((g1 + match) * 255),
            (byte)Math.Round((b1 + match) * 255));
    }

    public static HsvColor ToHsv(WpfColor color)
    {
        var red = color.R / 255d;
        var green = color.G / 255d;
        var blue = color.B / 255d;
        var max = Math.Max(red, Math.Max(green, blue));
        var min = Math.Min(red, Math.Min(green, blue));
        var delta = max - min;

        var hue = delta switch
        {
            0 => 0,
            _ when max == red => 60 * (((green - blue) / delta) % 6),
            _ when max == green => 60 * (((blue - red) / delta) + 2),
            _ => 60 * (((red - green) / delta) + 4)
        };

        if (hue < 0)
        {
            hue += 360;
        }

        var saturation = max == 0 ? 0 : delta / max;
        return new HsvColor(hue, saturation, max);
    }
}
