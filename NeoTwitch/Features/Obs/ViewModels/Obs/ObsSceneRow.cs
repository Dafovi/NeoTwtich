using System.Windows;

namespace NeoTwitch.ViewModels.Obs;

public sealed record ObsSceneRow(
    string Name,
    bool IsCurrent,
    string ShortName)
{
    public string StatusText => IsCurrent ? "Actual" : "Disponible";

    public Visibility ChangeButtonVisibility => IsCurrent ? Visibility.Collapsed : Visibility.Visible;
}
