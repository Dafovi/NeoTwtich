using System.Windows;
using System.Windows.Controls;
using NeoTwitch.Services.Navigation;
using NeoTwitch.Services.Ui;

namespace NeoTwitch;

public partial class MainWindow
{
    private void UpdateNavigationButtons()
    {
        if (_initializingComponent)
        {
            return;
        }

        UpdateServiceNavigationVisibility();

        var palette = _config.DarkMode
            ? ThemePalette.Dark
            : ThemePalette.Light;

        foreach (var button in new[] { NavSettingsButton, NavConnectionsButton, NavRulesButton, NavStripsButton, NavAlexaButton, NavAudioButton, NavImagesButton, NavVideosButton, NavObsButton, NavPreferencesButton, NavActivityButton })
        {
            ApplyNavigationButtonTheme(button, palette);
        }
    }

    private void UpdateServiceNavigationVisibility()
    {
        if (_initializingComponent)
        {
            return;
        }

        var visibility = ServiceNavigationVisibilityService.Resolve(_config);
        SetNavigationTargetVisible(NavStripsButton, LightsTab, visibility.Lights);
        SetNavigationTargetVisible(NavAlexaButton, AlexaTab, visibility.Alexa);
        SetNavigationTargetVisible(NavObsButton, ObsTab, visibility.Obs);
        SetNavigationTargetVisible(NavImagesButton, ImagesTab, visibility.Images);
        SetNavigationTargetVisible(NavVideosButton, VideosTab, visibility.Videos);

        if (MainTabs.SelectedItem is TabItem { Visibility: not Visibility.Visible })
        {
            MainTabs.SelectedItem = ConnectionsTab;
        }
    }

    private static void SetNavigationTargetVisible(
        System.Windows.Controls.Button button,
        TabItem tab,
        bool isVisible)
    {
        var visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
        button.Visibility = visibility;
        tab.Visibility = visibility;
    }

    private void ApplyNavigationButtonTheme(System.Windows.Controls.Button button, ThemePalette palette)
    {
        var isSelected = int.TryParse(button.Tag?.ToString(), out var index)
            && index == MainTabs.SelectedIndex;

        NavigationButtonThemeService.Apply(button, palette, isSelected);
    }
}
