using System.Collections.ObjectModel;
using NeoTwitch.Services.Lights;
using NeoTwitch.ViewModels.Ui;
using static NeoTwitch.Services.Ui.UiBrushFactory;

namespace NeoTwitch;

public partial class MainWindow
{
    private static RuleLedPreviewDot PreviewDot(System.Windows.Media.Color color, double brightness)
    {
        var glowOpacity = Math.Clamp(0.12 + (brightness * 0.72), 0.12, 0.9);
        var glowRadius = 7d + (brightness * 22d);
        return new RuleLedPreviewDot(
            FrozenBrushFrom($"#{color.R:X2}{color.G:X2}{color.B:X2}"),
            color,
            glowOpacity,
            glowRadius);
    }

    private static void ResizeLedPreviewDots(ObservableCollection<RuleLedPreviewDot> dots, double availableWidth)
    {
        var targetCount = LedPreviewService.CalculateDotCount(availableWidth);
        while (dots.Count < targetCount)
        {
            dots.Add(PreviewDot(LedPreviewService.ParseColor("#334155", "#334155"), 0.08));
        }

        while (dots.Count > targetCount)
        {
            dots.RemoveAt(dots.Count - 1);
        }
    }
}
