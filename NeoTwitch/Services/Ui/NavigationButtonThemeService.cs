using WpfButton = System.Windows.Controls.Button;
using WpfBrushes = System.Windows.Media.Brushes;

namespace NeoTwitch.Services.Ui;

public static class NavigationButtonThemeService
{
    public static void Apply(WpfButton button, ThemePalette palette, bool selected)
    {
        button.Background = selected
            ? palette.NavSelected
            : WpfBrushes.Transparent;
        button.Foreground = selected
            ? WpfBrushes.White
            : palette.SidebarMutedText;
        button.BorderBrush = WpfBrushes.Transparent;
    }
}
