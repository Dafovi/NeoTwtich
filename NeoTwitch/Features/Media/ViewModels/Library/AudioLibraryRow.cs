using System.Windows;
using System.Windows.Media;

namespace NeoTwitch.ViewModels.Library;

public sealed record AudioLibraryRow(
    string Id,
    string Name,
    string FilePath,
    string GroupId,
    string AssignedAlertText,
    string GroupName,
    string DurationText,
    bool HasAssignedAlert,
    bool IsPreviewing,
    SolidColorBrush AssignedAlertBrush,
    SolidColorBrush AssignedAlertBackground,
    int Index)
{
    public Visibility AssignedAlertVisibility => HasAssignedAlert ? Visibility.Visible : Visibility.Collapsed;

    public Visibility PlayIconVisibility => IsPreviewing ? Visibility.Collapsed : Visibility.Visible;

    public Visibility PauseIconVisibility => IsPreviewing ? Visibility.Visible : Visibility.Collapsed;

    public string PlayToolTip => IsPreviewing ? "Detener audio" : "Reproducir audio";
}
