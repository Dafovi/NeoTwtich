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
        var sourceMode = _alertsViewModel.Editor.AudioSourceMode;
        ApplyRuleModeButtonTheme(RuleSingleAudioModeButton, sourceMode == AudioSourceMode.Single, UiAccentCatalog.AudioSingle, palette);
        ApplyRuleModeButtonTheme(RuleGroupAudioModeButton, sourceMode == AudioSourceMode.Group, UiAccentCatalog.AudioGroup, palette);
    }

    private void UpdateRuleObsMediaModeSelection()
    {
        if (_initializingComponent)
        {
            return;
        }

        var palette = _config.DarkMode ? ThemePalette.Dark : ThemePalette.Light;
        var imageSourceMode = _alertsViewModel.Editor.ObsImageSourceMode;
        var videoSourceMode = _alertsViewModel.Editor.ObsVideoSourceMode;

        ApplyRuleModeButtonTheme(RuleObsImageSingleMediaModeButton, imageSourceMode == MediaSourceMode.Single, UiAccentCatalog.ObsImage, palette);
        ApplyRuleModeButtonTheme(RuleObsImageGroupMediaModeButton, imageSourceMode == MediaSourceMode.Group, UiAccentCatalog.MediaGroup, palette);
        ApplyRuleModeButtonTheme(RuleObsVideoSingleMediaModeButton, videoSourceMode == MediaSourceMode.Single, UiAccentCatalog.ObsVideo, palette);
        ApplyRuleModeButtonTheme(RuleObsVideoGroupMediaModeButton, videoSourceMode == MediaSourceMode.Group, UiAccentCatalog.MediaGroup, palette);
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
