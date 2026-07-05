using System.Windows.Media;
using NeoTwitch.Services.Status;
using NeoTwitch.Services.Ui;

namespace NeoTwitch.ViewModels.Connections;

public sealed record ConnectionBadgeViewModel(
    string Text,
    SolidColorBrush ForegroundBrush,
    SolidColorBrush BackgroundBrush,
    SolidColorBrush BorderBrush)
{
    public static ConnectionBadgeViewModel From(ConnectionStateVisual visual)
    {
        return From(visual.Text, visual.Color);
    }

    public static ConnectionBadgeViewModel From(string text, string color)
    {
        return new ConnectionBadgeViewModel(
            text,
            UiBrushFactory.FrozenBrushFrom(color),
            UiBrushFactory.TranslucentBrushFrom(color),
            UiBrushFactory.FrozenBrushFrom(color));
    }
}
