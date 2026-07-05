using NeoTwitch.Models;
using WpfColor = System.Windows.Media.Color;
using WpfColorConverter = System.Windows.Media.ColorConverter;

namespace NeoTwitch.Services.Lights;

public static class LedPreviewService
{
    public const int DefaultDotCount = 24;
    public const int MinDotCount = 8;
    public const int MaxDotCount = 36;

    public static int CalculateDotCount(double availableWidth)
    {
        if (double.IsNaN(availableWidth) || availableWidth <= 0)
        {
            return DefaultDotCount;
        }

        return Math.Clamp((int)Math.Floor(availableWidth / 32d), MinDotCount, MaxDotCount);
    }

    public static WpfColor[] BuildFrame(
        LightPattern pattern,
        int step,
        int count,
        double brightness,
        WpfColor primary,
        WpfColor secondary,
        WpfColor tertiary,
        Random random)
    {
        count = Math.Max(0, count);
        var frame = new WpfColor[count];
        if (count == 0)
        {
            return frame;
        }

        var colorScale = Math.Clamp(brightness, 0.08, 1d);
        for (var i = 0; i < count; i++)
        {
            var phase = (i + step) / (double)count;
            var color = pattern switch
            {
                LightPattern.Solid => primary,
                LightPattern.Rainbow => RainbowColor(phase),
                LightPattern.Pulse => Blend(primary, secondary, (Math.Sin((step * 0.18) + (i * 0.22)) + 1d) / 2d),
                LightPattern.Chase => ((i + step) % 6) < 2
                    ? primary
                    : Scale(secondary, 0.22),
                LightPattern.Theater => ((i + step) % 3) == 0
                    ? primary
                    : (((i + step) % 3) == 1 ? secondary : Scale(tertiary, 0.18)),
                LightPattern.Sparkle => random.NextDouble() > 0.72
                    ? PickRandom(primary, secondary, tertiary, random)
                    : Scale(primary, 0.16),
                LightPattern.Rave => PickRandom(primary, secondary, tertiary, random),
                _ => primary
            };

            frame[i] = Scale(color, colorScale);
        }

        return frame;
    }

    public static WpfColor ParseColor(string color, string fallback)
    {
        try
        {
            return (WpfColor)WpfColorConverter.ConvertFromString(LightCommand.NormalizeColor(color));
        }
        catch
        {
            return (WpfColor)WpfColorConverter.ConvertFromString(fallback);
        }
    }

    public static WpfColor Scale(WpfColor color, double factor)
    {
        factor = Math.Clamp(factor, 0d, 1d);
        return WpfColor.FromRgb(
            (byte)Math.Round(color.R * factor),
            (byte)Math.Round(color.G * factor),
            (byte)Math.Round(color.B * factor));
    }

    public static WpfColor Blend(WpfColor start, WpfColor end, double amount)
    {
        amount = Math.Clamp(amount, 0d, 1d);
        return WpfColor.FromRgb(
            (byte)Math.Round(start.R + ((end.R - start.R) * amount)),
            (byte)Math.Round(start.G + ((end.G - start.G) * amount)),
            (byte)Math.Round(start.B + ((end.B - start.B) * amount)));
    }

    public static WpfColor RainbowColor(double phase)
    {
        phase -= Math.Floor(phase);
        var h = phase * 6d;
        var x = 1d - Math.Abs((h % 2d) - 1d);
        var (r, g, b) = h switch
        {
            < 1d => (1d, x, 0d),
            < 2d => (x, 1d, 0d),
            < 3d => (0d, 1d, x),
            < 4d => (0d, x, 1d),
            < 5d => (x, 0d, 1d),
            _ => (1d, 0d, x)
        };

        return WpfColor.FromRgb(
            (byte)Math.Round(r * 255d),
            (byte)Math.Round(g * 255d),
            (byte)Math.Round(b * 255d));
    }

    private static WpfColor PickRandom(WpfColor primary, WpfColor secondary, WpfColor tertiary, Random random)
    {
        return random.Next(3) switch
        {
            0 => primary,
            1 => secondary,
            _ => tertiary
        };
    }
}
