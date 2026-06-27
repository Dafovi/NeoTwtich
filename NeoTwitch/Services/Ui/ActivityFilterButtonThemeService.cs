using System.Windows.Controls.Primitives;
using NeoTwitch.ViewModels.Activity;

namespace NeoTwitch.Services.Ui;

public static class ActivityFilterButtonThemeService
{
    public static void Apply(ToggleButton button, ThemePalette palette)
    {
        var filter = button.Tag?.ToString() ?? "";
        FilterButtonThemeService.Apply(
            button,
            button.IsChecked == true,
            ActivityLogVisuals.FilterAccent(filter),
            palette,
            inactiveForeground: palette.MutedText);
    }
}
