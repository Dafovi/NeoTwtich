using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using NeoTwitch.Services.Activity;
using NeoTwitch.Services.Ui;
using NeoTwitch.ViewModels.Activity;

namespace NeoTwitch;

public partial class MainWindow
{
    internal void ActivityFilterButton_CheckedChanged(object sender, RoutedEventArgs e)
    {
        if (_initializingComponent || sender is not ToggleButton button)
        {
            return;
        }

        var filter = button.Tag?.ToString() ?? "";
        if (string.IsNullOrWhiteSpace(filter))
        {
            return;
        }

        _activityViewModel.SetFilter(filter, button.IsChecked == true);

        ApplyActivityFilterButtonTheme(button, _config.DarkMode ? ThemePalette.Dark : ThemePalette.Light);
    }

    internal void ActivitySearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_loadingUi || sender is not System.Windows.Controls.TextBox textBox)
        {
            return;
        }

        _activityViewModel.SearchText = textBox.Text;
    }

    internal void ClearActivityFiltersButton_Click(object sender, RoutedEventArgs e)
    {
        _activityViewModel.ClearFilters();
        foreach (var button in ActivityFilterButtons())
        {
            button.IsChecked = true;
            ApplyActivityFilterButtonTheme(button, _config.DarkMode ? ThemePalette.Dark : ThemePalette.Light);
        }

        ActivitySearchBox.Text = "";
    }

    internal void ClearActivityHistoryButton_Click(object sender, RoutedEventArgs e)
    {
        _activityViewModel.ClearHistory();
    }

    private void ApplyActivityFilterButtonTheme(ToggleButton button, ThemePalette palette)
    {
        var filter = button.Tag?.ToString() ?? "";
        FilterButtonThemeService.Apply(
            button,
            button.IsChecked == true,
            ActivityLogVisuals.FilterAccent(filter),
            palette,
            inactiveForeground: palette.MutedText);
    }

    private IEnumerable<ToggleButton> ActivityFilterButtons()
    {
        return
        [
            ActivityFilterTwitchButton,
            ActivityFilterArduinoButton,
            ActivityFilterAlexaButton,
            ActivityFilterAudioButton,
            ActivityFilterObsButton,
            ActivityFilterEventButton,
            ActivityFilterSystemButton,
            ActivityFilterImportantButton
        ];
    }

    private void AddLog(string message)
    {
        AddLog(message, ActivityLogClassifier.Classify(message));
    }

    private void AddLog(string message, ActivityLogKind kind)
    {
        Dispatcher.BeginInvoke(() =>
        {
            _activityLog.Add(message, kind);
        });
    }
}
