using NeoTwitch.Models;
using NeoTwitch.Services.Ui;
using NeoTwitch.ViewModels.Library;

namespace NeoTwitch;

public partial class MainWindow
{
    private void UpdateRuleAudioModeSelection()
    {
        if (_initializingComponent)
        {
            return;
        }

        var palette = _config.DarkMode ? ThemePalette.Dark : ThemePalette.Light;
        ApplyRuleModeButtonTheme(RuleSingleAudioModeButton, _ruleAudioMode == AudioSourceMode.Single, UiAccentCatalog.AudioSingle, palette);
        ApplyRuleModeButtonTheme(RuleGroupAudioModeButton, _ruleAudioMode == AudioSourceMode.Group, UiAccentCatalog.AudioGroup, palette);
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

        ApplyRuleModeButtonTheme(RuleObsImageModeButton, mediaKind == ObsMediaKind.Image, UiAccentCatalog.ObsImage, palette);
        ApplyRuleModeButtonTheme(RuleObsVideoModeButton, mediaKind == ObsMediaKind.Video, UiAccentCatalog.ObsVideo, palette);
        ApplyRuleModeButtonTheme(RuleObsSingleMediaModeButton, sourceMode == MediaSourceMode.Single, UiAccentCatalog.MediaSingle, palette);
        ApplyRuleModeButtonTheme(RuleObsGroupMediaModeButton, sourceMode == MediaSourceMode.Group, UiAccentCatalog.MediaGroup, palette);
    }

    private static void ApplyRuleModeButtonTheme(
        System.Windows.Controls.Button button,
        bool active,
        string accentColor,
        ThemePalette palette)
    {
        SelectionButtonThemeService.Apply(button, active, accentColor, palette);
    }
}
