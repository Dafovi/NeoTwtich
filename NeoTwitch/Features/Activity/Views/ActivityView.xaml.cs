using System.Windows;
using System.Windows.Controls.Primitives;
using NeoTwitch.Services.Ui;

namespace NeoTwitch.Views;

public partial class ActivityView : NeoTwitchView
{
    public ActivityView()
    {
        InitializeComponent();
        AddHandler(ToggleButton.CheckedEvent, new RoutedEventHandler(ActivityFilterButtonStateChanged));
        AddHandler(ToggleButton.UncheckedEvent, new RoutedEventHandler(ActivityFilterButtonStateChanged));
    }

    private void ActivityFilterButtonStateChanged(object sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is ToggleButton button
            && ActivityFilterButtonThemeService.IsActivityFilterButton(button))
        {
            ActivityFilterButtonThemeService.Apply(button, CurrentPalette());
        }
    }

    private ThemePalette CurrentPalette()
    {
        return ReferenceEquals(TryFindResource("ThemeWindowBrush"), ThemePalette.Dark.Window)
            ? ThemePalette.Dark
            : ThemePalette.Light;
    }
}
