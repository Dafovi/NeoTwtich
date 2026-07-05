using System.Windows;
using System.Windows.Media;
using NeoTwitch.Services.Ui;

namespace NeoTwitch;

public partial class MainWindow
{
    private void ApplyThemeToElement(DependencyObject element, ThemePalette palette)
    {
        if (TryApplyThemeToKnownElement(element, palette, out var skipChildren) && skipChildren)
        {
            return;
        }

        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(element); i++)
        {
            ApplyThemeToElement(VisualTreeHelper.GetChild(element, i), palette);
        }
    }
}
