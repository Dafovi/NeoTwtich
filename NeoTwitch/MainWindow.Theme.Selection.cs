using System.Windows.Controls.Primitives;
using NeoTwitch.Models;
using NeoTwitch.Services.Ui;
using NeoTwitch.ViewModels.Library;

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

    private void UpdateEventKindTileSelection()
    {
        if (_initializingComponent)
        {
            return;
        }

        var selectedKind = EventKindBox.SelectedValue is TwitchEventKind kind
            ? kind
            : TwitchEventKind.Follow;
        var palette = _config.DarkMode
            ? ThemePalette.Dark
            : ThemePalette.Light;

        foreach (var button in EventKindTileButtons())
        {
            if (button.Tag is not string value || !Enum.TryParse<TwitchEventKind>(value, out var tileKind))
            {
                continue;
            }

            var selected = tileKind == selectedKind;
            SelectionButtonThemeService.Apply(
                button,
                selected,
                UiAccentCatalog.ForEventKind(tileKind),
                palette);
        }
    }

    private IEnumerable<System.Windows.Controls.Button> EventKindTileButtons()
    {
        return
        [
            EventFollowTileButton,
            EventSubscriptionTileButton,
            EventRaidTileButton,
            EventCheerTileButton,
            EventChatCommandTileButton,
            EventRedemptionTileButton
        ];
    }

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

    private void UpdateRuleAudioModeSelection()
    {
        if (_initializingComponent)
        {
            return;
        }

        var palette = _config.DarkMode ? ThemePalette.Dark : ThemePalette.Light;
        ApplyRuleAudioModeButtonTheme(RuleSingleAudioModeButton, _ruleAudioMode == AudioSourceMode.Single, UiAccentCatalog.AudioSingle, palette);
        ApplyRuleAudioModeButtonTheme(RuleGroupAudioModeButton, _ruleAudioMode == AudioSourceMode.Group, UiAccentCatalog.AudioGroup, palette);
    }

    private static void ApplyRuleAudioModeButtonTheme(System.Windows.Controls.Button button, bool active, string accentColor, ThemePalette palette)
    {
        SelectionButtonThemeService.Apply(button, active, accentColor, palette);
    }

    private void UpdateRuleObsMediaModeSelection()
    {
        if (_initializingComponent)
        {
            return;
        }

        var palette = _config.DarkMode ? ThemePalette.Dark : ThemePalette.Light;
        var mediaKind = RuleObsMediaKindBox.SelectedValue is ObsMediaKind kind
            ? kind
            : ObsMediaKind.Image;
        var sourceMode = RuleObsMediaSourceModeBox.SelectedValue is MediaSourceMode mode
            ? mode
            : MediaSourceMode.Single;

        ApplyRuleAudioModeButtonTheme(RuleObsImageModeButton, mediaKind == ObsMediaKind.Image, UiAccentCatalog.ObsImage, palette);
        ApplyRuleAudioModeButtonTheme(RuleObsVideoModeButton, mediaKind == ObsMediaKind.Video, UiAccentCatalog.ObsVideo, palette);
        ApplyRuleAudioModeButtonTheme(RuleObsSingleMediaModeButton, sourceMode == MediaSourceMode.Single, UiAccentCatalog.MediaSingle, palette);
        ApplyRuleAudioModeButtonTheme(RuleObsGroupMediaModeButton, sourceMode == MediaSourceMode.Group, UiAccentCatalog.MediaGroup, palette);
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
