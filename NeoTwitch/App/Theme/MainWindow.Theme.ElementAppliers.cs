using System.Windows;
using NeoTwitch.Services.Ui;
using WpfButton = System.Windows.Controls.Button;

namespace NeoTwitch;

public partial class MainWindow
{
    private bool TryApplyThemeToKnownElement(
        DependencyObject element,
        ThemePalette palette,
        out bool skipChildren)
    {
        if (ThemeElementApplicationService.TryApply(element, palette, out skipChildren))
        {
            return true;
        }

        if (element is WpfButton button)
        {
            ApplyButtonTheme(button, palette);
            skipChildren = true;
            return true;
        }

        skipChildren = false;
        return false;
    }
}
