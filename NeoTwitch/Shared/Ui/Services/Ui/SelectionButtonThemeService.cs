namespace NeoTwitch.Services.Ui;

using static UiBrushFactory;
using WpfBrush = System.Windows.Media.Brush;
using WpfButtonBase = System.Windows.Controls.Primitives.ButtonBase;

public static class SelectionButtonThemeService
{
    public static void Apply(
        WpfButtonBase button,
        bool selected,
        string accentColor,
        ThemePalette palette,
        WpfBrush? inactiveForeground = null,
        bool fillSelected = true)
    {
        var accent = FrozenBrushFrom(accentColor);

        button.Background = selected && fillSelected
            ? TranslucentBrushFrom(accentColor)
            : palette.Input;
        button.Foreground = selected
            ? accent
            : inactiveForeground ?? palette.Text;
        button.BorderBrush = selected
            ? accent
            : palette.Border;
    }
}
