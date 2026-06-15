using System.Windows;
using System.Windows.Controls;

namespace NeoTwitch.Views;

public partial class ActivityView : NeoTwitchView
{
    public ActivityView()
    {
        InitializeComponent();
    }

    private void ActivityFilterButton_CheckedChanged(object sender, RoutedEventArgs e)
    {
        Host?.ActivityFilterButton_CheckedChanged(sender, e);
    }

    private void ActivitySearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        Host?.ActivitySearchBox_TextChanged(sender, e);
    }

    private void ClearActivityFiltersButton_Click(object sender, RoutedEventArgs e)
    {
        Host?.ClearActivityFiltersButton_Click(sender, e);
    }

    private void ClearActivityHistoryButton_Click(object sender, RoutedEventArgs e)
    {
        Host?.ClearActivityHistoryButton_Click(sender, e);
    }
}
