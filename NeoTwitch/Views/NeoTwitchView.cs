using System.Windows;

namespace NeoTwitch.Views;

public abstract class NeoTwitchView : System.Windows.Controls.UserControl
{
    protected MainWindow? Host => Window.GetWindow(this) as MainWindow;
}
