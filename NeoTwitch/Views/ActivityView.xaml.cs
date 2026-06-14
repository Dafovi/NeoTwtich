using System.Windows;
using System.Windows.Controls;

namespace NeoTwitch.Views;

public partial class ActivityView : System.Windows.Controls.UserControl
{
    public ActivityView()
    {
        InitializeComponent();
    }

    private MainWindow? Host => Window.GetWindow(this) as MainWindow;

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
}
