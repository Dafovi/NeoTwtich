using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using NeoTwitch.Services.Status;

namespace NeoTwitch.Services.Ui;

using static UiBrushFactory;

public static class ConnectionVisualThemeService
{
    public static void ApplyDashboardState(
        TextBlock stateText,
        Border statusIcon,
        ConnectionStateVisual visual)
    {
        var brush = FrozenBrushFrom(visual.Color);

        stateText.Text = visual.Text;
        stateText.Foreground = brush;
        statusIcon.Background = brush;
        statusIcon.OpacityMask = new ImageBrush
        {
            ImageSource = PackImageLoader.Load(visual.IconPath),
            Stretch = Stretch.Uniform
        };
        statusIcon.ToolTip = visual.Text;
    }

    public static void ApplyConnectionBadge(
        Border badge,
        TextBlock textBlock,
        ConnectionStateVisual visual)
    {
        var brush = FrozenBrushFrom(visual.Color);

        textBlock.Text = visual.Text;
        textBlock.Foreground = brush;
        badge.Background = TranslucentBrushFrom(visual.Color);
        badge.BorderBrush = brush;
        badge.BorderThickness = new Thickness(1);
    }
}
