using System.Windows;
using System.Windows.Controls;
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

        SetNavigationTargetVisible(NavStripsButton, LightsTab, _config.ArduinoEnabled);
        SetNavigationTargetVisible(NavAlexaButton, AlexaTab, _config.Alexa.Enabled);
        SetNavigationTargetVisible(NavObsButton, ObsTab, _config.Obs.Enabled);
        SetNavigationTargetVisible(NavImagesButton, ImagesTab, _config.Obs.Enabled);
        SetNavigationTargetVisible(NavVideosButton, VideosTab, _config.Obs.Enabled);

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

        button.Background = isSelected
            ? palette.NavSelected
            : System.Windows.Media.Brushes.Transparent;
        button.Foreground = isSelected
            ? System.Windows.Media.Brushes.White
            : palette.SidebarMutedText;
        button.BorderBrush = System.Windows.Media.Brushes.Transparent;
    }
}
