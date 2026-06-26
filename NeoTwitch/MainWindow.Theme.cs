using NeoTwitch.Services;
using NeoTwitch.Services.Ui;
using NeoTwitch.ViewModels.Library;

namespace NeoTwitch;

public partial class MainWindow
{
    private void ApplyTheme()
    {
        _config.DarkMode = ThemeModeService.ResolveDarkMode(_config.ThemeMode);
        var palette = _config.DarkMode
            ? ThemePalette.Dark
            : ThemePalette.Light;

        Background = palette.Window;
        ThemeResourceService.Apply(Resources, palette);
        ApplyWindowChromeColor();
        UpdateNavigationButtons();
        ApplyBackgroundOutputMode();
        ApplyThemeToElement(this, palette);
        ApplyBackgroundOutputMode();
        UpdateTwitchLiveIndicator();
        UpdateDashboardSummary();
        UpdateColorButtons();
        UpdateEventKindTileSelection();
        UpdatePatternTileSelection();
        UpdateBackgroundPatternTileSelection();
        UpdateRuleAudioModeSelection();
        UpdateRuleObsMediaModeSelection();
        UpdateAudioFilterButtons();
        UpdateMediaFilterButtons(MediaLibraryKind.Image);
        UpdateMediaFilterButtons(MediaLibraryKind.Video);
        UpdateCloseBehaviorCards();
    }

}
