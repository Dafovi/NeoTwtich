using System.Windows.Media;

namespace NeoTwitch.ViewModels.Library;

public sealed record AudioGroupRow(
    string Id,
    string Name,
    string CountText,
    SolidColorBrush AccentBrush);
