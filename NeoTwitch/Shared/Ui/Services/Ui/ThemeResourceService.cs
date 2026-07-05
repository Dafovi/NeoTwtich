using System.Windows;

namespace NeoTwitch.Services.Ui;

using WpfBrushes = System.Windows.Media.Brushes;
using WpfSystemColors = System.Windows.SystemColors;

public static class ThemeResourceService
{
    public static void Apply(ResourceDictionary resources, ThemePalette palette)
    {
        resources["ThemeWindowBrush"] = palette.Window;
        resources["ThemeSidebarBrush"] = palette.Sidebar;
        resources["ThemeSurfaceBrush"] = palette.Surface;
        resources["ThemeButtonBrush"] = palette.Button;
        resources["ThemeTextBrush"] = palette.Text;
        resources["ThemeMutedTextBrush"] = palette.MutedText;
        resources["ThemeSidebarTextBrush"] = palette.SidebarText;
        resources["ThemeSidebarMutedTextBrush"] = palette.SidebarMutedText;
        resources["ThemeInputBrush"] = palette.Input;
        resources["ThemeBorderBrush"] = palette.Border;
        resources["ThemeSelectionBrush"] = palette.Accent;
        resources["ThemeConsoleBrush"] = palette.Console;
        resources["ThemeScrollThumbBrush"] = palette.Accent;
        resources["ThemeScrollTrackBrush"] = palette.ScrollTrack;
        resources[WpfSystemColors.WindowBrushKey] = palette.Input;
        resources[WpfSystemColors.ControlBrushKey] = palette.Input;
        resources[WpfSystemColors.WindowTextBrushKey] = palette.Text;
        resources[WpfSystemColors.ControlTextBrushKey] = palette.Text;
        resources[WpfSystemColors.HighlightBrushKey] = palette.Accent;
        resources[WpfSystemColors.HighlightTextBrushKey] = WpfBrushes.White;
        resources[WpfSystemColors.InactiveSelectionHighlightBrushKey] = palette.Accent;
        resources[WpfSystemColors.InactiveSelectionHighlightTextBrushKey] = WpfBrushes.White;
    }
}
