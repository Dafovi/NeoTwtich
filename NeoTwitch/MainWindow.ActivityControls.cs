using System.Windows;
using System.Windows.Controls.Primitives;

namespace NeoTwitch;

public partial class MainWindow
{
    private System.Windows.Controls.ListBox ActivityList => ActivityView.ActivityList;

    private void ClearLogButton_Click(object sender, RoutedEventArgs e)
    {
        _activityLog.Clear();
    }
}
