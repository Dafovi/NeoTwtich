using System.Windows.Controls.Primitives;

namespace NeoTwitch.Services.Ui;

public static class RuleStatusFilterButtonThemeService
{
    public static bool IsRuleStatusFilterButton(ToggleButton button)
    {
        return button.Name.StartsWith("RuleFilter", StringComparison.OrdinalIgnoreCase);
    }

    public static void Apply(ToggleButton button, ThemePalette palette)
    {
        var active = button.IsChecked == true;
        var accentColor = button.Tag?.ToString() switch
        {
            "ACTIVE" => "#22C55E",
            "INACTIVE" => "#94A3B8",
            _ => "#14B8A6"
        };

        SelectionButtonThemeService.Apply(
            button,
            active,
            accentColor,
            palette,
            inactiveForeground: palette.MutedText);
    }
}
