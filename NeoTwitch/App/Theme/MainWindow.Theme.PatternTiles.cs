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

        var selectedPattern = _alertsViewModel.Editor.Pattern;
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

        UpdateVirtualPatternTileSelection();
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

    private void UpdateVirtualPatternTileSelection()
    {
        if (_initializingComponent)
        {
            return;
        }

        var selectedPattern = _alertsViewModel.Editor.VirtualLightsPattern;
        var palette = _config.DarkMode
            ? ThemePalette.Dark
            : ThemePalette.Light;

        foreach (var button in VirtualPatternTileButtons())
        {
            if (button.Tag is not string value)
            {
                continue;
            }

            var raw = value.StartsWith("Virtual:", StringComparison.OrdinalIgnoreCase)
                ? value["Virtual:".Length..]
                : value;
            if (!Enum.TryParse<LightPattern>(raw, out var tilePattern))
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

    private IEnumerable<System.Windows.Controls.Button> VirtualPatternTileButtons()
    {
        return
        [
            VirtualPatternSolidTileButton,
            VirtualPatternPulseTileButton,
            VirtualPatternRainbowTileButton,
            VirtualPatternChaseTileButton,
            VirtualPatternTheaterTileButton,
            VirtualPatternSparkleTileButton,
            VirtualPatternRaveTileButton
        ];
    }

    private void UpdateBackgroundPatternTileSelection()
    {
        if (_initializingComponent)
        {
            return;
        }

        var selectedPattern = _lightsViewModel.BackgroundPattern;
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
