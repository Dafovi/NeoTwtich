using System.Windows;

namespace NeoTwitch;

public partial class MainWindow
{
    private void ClearLogButton_Click(object sender, RoutedEventArgs e)
    {
        _activityLog.Clear();
    }
}
