using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using NeoTwitch.Services.Activity;
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

        if (button.IsChecked == true)
        {
            _activityEnabledFilters.Add(filter);
        }
        else
        {
            _activityEnabledFilters.Remove(filter);
        }

        ApplyActivityFilterButtonTheme(button, _config.DarkMode ? ThemePalette.Dark : ThemePalette.Light);
        _activityViewSource.View?.Refresh();
    }

    internal void ActivitySearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_loadingUi || sender is not System.Windows.Controls.TextBox textBox)
        {
            return;
        }

        _activitySearchText = textBox.Text.Trim();
        _activityViewSource.View?.Refresh();
    }

    internal void ClearActivityFiltersButton_Click(object sender, RoutedEventArgs e)
    {
        _activityEnabledFilters.Clear();
        foreach (var button in ActivityFilterButtons())
        {
            var filter = button.Tag?.ToString() ?? "";
            if (!string.IsNullOrWhiteSpace(filter))
            {
                _activityEnabledFilters.Add(filter);
            }

            button.IsChecked = true;
            ApplyActivityFilterButtonTheme(button, _config.DarkMode ? ThemePalette.Dark : ThemePalette.Light);
        }

        ActivitySearchBox.Text = "";
        _activitySearchText = "";
        _activityViewSource.View?.Refresh();
    }

    internal void ClearActivityHistoryButton_Click(object sender, RoutedEventArgs e)
    {
        _activity.Clear();
        _dashboardActivity.Clear();
        AddLog("Actividad: historial borrado.", ActivityLogKind.Info);
    }

    private void ActivityViewSource_Filter(object sender, FilterEventArgs e)
    {
        if (e.Item is not ActivityLogEntry entry)
        {
            e.Accepted = false;
            return;
        }

        e.Accepted = entry.MatchesFilter(_activityEnabledFilters, _activitySearchText);
    }

    private void ApplyActivityFilterButtonTheme(ToggleButton button, ThemePalette palette)
    {
        var filter = button.Tag?.ToString() ?? "";
        var accentColor = ActivityFilterAccent(filter);
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

    private static string ActivityFilterAccent(string filter)
    {
        return ActivityLogVisuals.FilterAccent(filter);
    }

    private void AddLog(string message)
    {
        AddLog(message, ActivityLogClassifier.Classify(message));
    }

    private void AddLog(string message, ActivityLogKind kind)
    {
        Dispatcher.BeginInvoke(() =>
        {
            var entry = new ActivityLogEntry(message, kind);
            _activity.Insert(0, entry);
            _dashboardActivity.Insert(0, entry);

            while (_activity.Count > 250)
            {
                _activity.RemoveAt(_activity.Count - 1);
            }

            while (_dashboardActivity.Count > 10)
            {
                _dashboardActivity.RemoveAt(_dashboardActivity.Count - 1);
            }
        });
    }
}
