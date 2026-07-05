using System.Windows.Media;
using System.Windows;

namespace NeoTwitch.ViewModels.Library;

public sealed record MediaLibraryRow(
    string Id,
    string Name,
    string FilePath,
    string GroupId,
    string GroupName,
    string MetadataText,
    string IconPath,
    SolidColorBrush AccentBrush,
    SolidColorBrush AccentBackground,
    int Index,
    bool CanPreview,
    bool IsPreviewing)
{
    public double PreviewOpacity => CanPreview ? 1d : 0.42d;

    public Visibility PlayIconVisibility => IsPreviewing ? Visibility.Collapsed : Visibility.Visible;

    public Visibility PauseIconVisibility => IsPreviewing ? Visibility.Visible : Visibility.Collapsed;

    public string PlayToolTip => IsPreviewing ? "Detener prueba en OBS" : "Probar en OBS";
}
