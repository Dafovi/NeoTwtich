using System.Windows.Controls.Primitives;
using NeoTwitch.ViewModels.Activity;

namespace NeoTwitch.Services.Ui;

public static class ActivityFilterButtonThemeService
{
    private static readonly HashSet<string> ActivityFilterTags = new(StringComparer.OrdinalIgnoreCase)
    {
        "TWITCH",
        "ARDUINO",
        "ALEXA",
        "AUDIO",
        "OBS",
        "EVENTO",
        "SISTEMA",
        "IMPORTANTE"
    };

    public static bool IsActivityFilterButton(ToggleButton button)
    {
        return ActivityFilterTags.Contains(button.Tag?.ToString() ?? "");
    }

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
