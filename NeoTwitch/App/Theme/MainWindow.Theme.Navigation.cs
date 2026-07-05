using System.Windows;
using System.Windows.Controls;
using NeoTwitch.Services.Ui;
using NeoTwitch.ViewModels.Shell;

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

        _shellViewModel.ApplyServiceVisibility(_config);
        SetNavigationTargetVisible(NavStripsButton, LightsTab, ShellViewModel.LightsTabIndex);
        SetNavigationTargetVisible(NavAlexaButton, AlexaTab, ShellViewModel.AlexaTabIndex);
        SetNavigationTargetVisible(NavObsButton, ObsTab, ShellViewModel.ObsTabIndex);
        SetNavigationTargetVisible(NavImagesButton, ImagesTab, ShellViewModel.ImagesTabIndex);
        SetNavigationTargetVisible(NavVideosButton, VideosTab, ShellViewModel.VideosTabIndex);

        if (MainTabs.SelectedItem is TabItem { Visibility: not Visibility.Visible })
        {
            _shellViewModel.NavigateTo(ShellViewModel.ConnectionsTabIndex);
        }
    }

    private void SetNavigationTargetVisible(
        System.Windows.Controls.Button button,
        TabItem tab,
        int tabIndex)
    {
        var isVisible = _shellViewModel.FindByIndex(tabIndex)?.IsVisible == true;
        var visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
        button.Visibility = visibility;
        tab.Visibility = visibility;
    }

    private void ApplyNavigationButtonTheme(System.Windows.Controls.Button button, ThemePalette palette)
    {
        var isSelected = int.TryParse(button.Tag?.ToString(), out var index)
            && _shellViewModel.FindByIndex(index)?.IsSelected == true;

        NavigationButtonThemeService.Apply(button, palette, isSelected);
    }
}
