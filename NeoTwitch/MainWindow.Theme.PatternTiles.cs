using NeoTwitch.Models;
using NeoTwitch.Services.Ui;

namespace NeoTwitch;

public partial class MainWindow
{
    private void UpdatePatternTileSelection()
    {
        if (_initializingComponent)
        {
            return;
        }

        var selectedPattern = PatternBox.SelectedValue is LightPattern pattern
            ? pattern
            : LightPattern.Pulse;
        var palette = _config.DarkMode
            ? ThemePalette.Dark
            : ThemePalette.Light;

        foreach (var button in PatternTileButtons())
        {
            if (button.Tag is not string value || !Enum.TryParse<LightPattern>(value, out var tilePattern))
            {
                continue;
            }

            var selected = tilePattern == selectedPattern;
            SelectionButtonThemeService.Apply(
                button,
                selected,
                UiAccentCatalog.ForLightPattern(tilePattern),
                palette,
                fillSelected: false);
        }
    }

    private IEnumerable<System.Windows.Controls.Button> PatternTileButtons()
    {
        return
        [
            PatternSolidTileButton,
            PatternPulseTileButton,
            PatternRainbowTileButton,
            PatternChaseTileButton,
            PatternTheaterTileButton,
            PatternSparkleTileButton,
            PatternRaveTileButton
        ];
    }

    private void UpdateBackgroundPatternTileSelection()
    {
        if (_initializingComponent)
        {
            return;
        }

        var selectedPattern = BackgroundPatternBox.SelectedValue is LightPattern pattern
            ? pattern
            : LightPattern.Solid;
        var palette = _config.DarkMode
            ? ThemePalette.Dark
            : ThemePalette.Light;

        foreach (var button in BackgroundPatternTileButtons())
        {
            if (button.Tag is not string value || !Enum.TryParse<LightPattern>(value, out var tilePattern))
            {
                continue;
            }

            var selected = tilePattern == selectedPattern;
            SelectionButtonThemeService.Apply(
                button,
                selected,
                UiAccentCatalog.ForLightPattern(tilePattern),
                palette,
                fillSelected: false);
        }
    }

    private IEnumerable<System.Windows.Controls.Button> BackgroundPatternTileButtons()
    {
        return
        [
            BackgroundPatternSolidTileButton,
            BackgroundPatternPulseTileButton,
            BackgroundPatternRainbowTileButton,
            BackgroundPatternChaseTileButton,
            BackgroundPatternTheaterTileButton,
            BackgroundPatternSparkleTileButton,
            BackgroundPatternRaveTileButton
        ];
    }
}
