using System.Windows.Controls.Primitives;
using System.Windows.Media;
using static NeoTwitch.Services.Ui.UiBrushFactory;
using WpfBrush = System.Windows.Media.Brush;
using WpfButtonBase = System.Windows.Controls.Primitives.ButtonBase;

namespace NeoTwitch.Services.Ui;

public static class FilterButtonThemeService
{
    public static void Apply(
        WpfButtonBase button,
        bool active,
        string accentColor,
        ThemePalette palette,
        WpfBrush? inactiveForeground = null)
    {
        var accent = FrozenBrushFrom(accentColor);

        button.Background = active
            ? TranslucentBrushFrom(accentColor)
            : palette.Input;
        button.Foreground = active
            ? accent
            : inactiveForeground ?? palette.Text;
        button.BorderBrush = active
            ? accent
            : palette.Border;
    }
}
