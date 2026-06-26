using System.Windows.Controls.Primitives;
using NeoTwitch.Services.Ui;

namespace NeoTwitch;

public partial class MainWindow
{
    private void ApplyRuleStatusFilterButtonTheme(ToggleButton button, ThemePalette palette)
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

    private static bool IsRuleStatusFilterButton(ToggleButton button)
    {
        return button.Name.StartsWith("RuleFilter", StringComparison.OrdinalIgnoreCase);
    }

    private IEnumerable<ToggleButton> RuleStatusFilterButtons()
    {
        return
        [
            RuleFilterAllButton,
            RuleFilterActiveButton,
            RuleFilterInactiveButton
        ];
    }
}
