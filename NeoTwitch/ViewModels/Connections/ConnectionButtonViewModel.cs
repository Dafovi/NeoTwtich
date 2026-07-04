using System.Windows.Media;
using NeoTwitch.Services.Status;
using NeoTwitch.Services.Ui;

namespace NeoTwitch.ViewModels.Connections;

public sealed record ConnectionButtonViewModel(
    bool IsEnabled,
    string Text,
    Geometry IconGeometry)
{
    public static ConnectionButtonViewModel From(ConnectionButtonState state)
    {
        return From(state.Content, state.IconKey, state.IsEnabled);
    }

    public static ConnectionButtonViewModel From(string text, string iconKey, bool isEnabled)
    {
        var geometry = Geometry.Parse(IconPathCatalog.Get(iconKey));
        geometry.Freeze();
        return new ConnectionButtonViewModel(isEnabled, text, geometry);
    }
}
