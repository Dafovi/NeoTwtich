using System.Windows;
using System.Windows.Controls.Primitives;
using NeoTwitch.ViewModels.Activity;

namespace NeoTwitch.Views;

public partial class ActivityView : NeoTwitchView
{
    public ActivityView()
    {
        InitializeComponent();
    }

    private void ActivityFilterButton_CheckedChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is not ActivityViewModel viewModel || sender is not ToggleButton button)
        {
            return;
        }

        var filter = button.Tag?.ToString() ?? "";
        viewModel.SetFilter(filter, button.IsChecked == true);
        Host?.RefreshActivityFilterButtonTheme(button);
    }

    private void ClearActivityFiltersButton_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not ActivityViewModel viewModel)
        {
            return;
        }

        viewModel.ClearFilters();
        foreach (var button in ActivityFilterButtons())
        {
            button.IsChecked = true;
            Host?.RefreshActivityFilterButtonTheme(button);
        }
    }

    private void ClearActivityHistoryButton_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is ActivityViewModel viewModel)
        {
            viewModel.ClearHistory();
        }
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
}
