using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using NeoTwitch.Services.Activity;
using NeoTwitch.ViewModels.Activity;
using static NeoTwitch.Services.Ui.UiBrushFactory;

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

        _activityLog.SetFilter(filter, button.IsChecked == true);

        ApplyActivityFilterButtonTheme(button, _config.DarkMode ? ThemePalette.Dark : ThemePalette.Light);
        _activityViewSource.View?.Refresh();
    }

    internal void ActivitySearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_loadingUi || sender is not System.Windows.Controls.TextBox textBox)
        {
            return;
        }

        _activityLog.SetSearchText(textBox.Text);
        _activityViewSource.View?.Refresh();
    }

    internal void ClearActivityFiltersButton_Click(object sender, RoutedEventArgs e)
    {
        _activityLog.ResetFilters();
        foreach (var button in ActivityFilterButtons())
        {
            button.IsChecked = true;
            ApplyActivityFilterButtonTheme(button, _config.DarkMode ? ThemePalette.Dark : ThemePalette.Light);
        }

        ActivitySearchBox.Text = "";
        _activityLog.SetSearchText("");
        _activityViewSource.View?.Refresh();
    }

    internal void ClearActivityHistoryButton_Click(object sender, RoutedEventArgs e)
    {
        _activityLog.Clear();
    }

    private void ActivityViewSource_Filter(object sender, FilterEventArgs e)
    {
        if (e.Item is not ActivityLogEntry entry)
        {
            e.Accepted = false;
            return;
        }

        e.Accepted = _activityLog.Matches(entry);
    }

    private void ApplyActivityFilterButtonTheme(ToggleButton button, ThemePalette palette)
    {
        var filter = button.Tag?.ToString() ?? "";
        var accentColor = ActivityLogVisuals.FilterAccent(filter);
        var accent = FrozenBrushFrom(accentColor);
        var active = button.IsChecked == true;

        button.Background = active
            ? TranslucentBrushFrom(accentColor)
            : palette.Input;
        button.Foreground = active
            ? accent
            : palette.MutedText;
        button.BorderBrush = active
            ? accent
            : palette.Border;
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
