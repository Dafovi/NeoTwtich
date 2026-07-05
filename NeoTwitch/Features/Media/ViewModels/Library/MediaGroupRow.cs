using System.Windows.Media;

namespace NeoTwitch.ViewModels.Library;

public sealed record MediaGroupRow(
    string Id,
    string Name,
    string CountText,
    SolidColorBrush AccentBrush);
