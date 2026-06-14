using System.Windows.Media;

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
    bool CanPreview)
{
    public double PreviewOpacity => CanPreview ? 1d : 0.42d;
}
