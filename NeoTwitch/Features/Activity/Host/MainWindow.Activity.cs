using System.Windows.Controls.Primitives;
using NeoTwitch.Services.Activity;
using NeoTwitch.Services.Ui;
using NeoTwitch.ViewModels.Activity;

namespace NeoTwitch;

public partial class MainWindow
{
    internal void RefreshActivityFilterButtonTheme(ToggleButton button)
    {
        ActivityFilterButtonThemeService.Apply(button, _config.DarkMode ? ThemePalette.Dark : ThemePalette.Light);
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
