using System.Windows;

namespace NeoTwitch.Views;

public partial class DashboardView : System.Windows.Controls.UserControl
{
    public DashboardView()
    {
        InitializeComponent();
    }

    private MainWindow? Host => Window.GetWindow(this) as MainWindow;

    private void GoToActivityButton_Click(object sender, RoutedEventArgs e)
    {
        Host?.GoToActivityButton_Click(sender, e);
    }
}
