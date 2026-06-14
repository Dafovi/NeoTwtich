using System.Windows;

namespace NeoTwitch.Views;

public partial class DashboardView : NeoTwitchView
{
    public DashboardView()
    {
        InitializeComponent();
    }

    private void GoToActivityButton_Click(object sender, RoutedEventArgs e)
    {
        Host?.GoToActivityButton_Click(sender, e);
    }
}
