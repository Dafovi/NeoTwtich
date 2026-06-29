using WpfComboBox = System.Windows.Controls.ComboBox;
using WpfTextBox = System.Windows.Controls.TextBox;

namespace NeoTwitch;

public partial class MainWindow
{
    private WpfTextBox ObsOverlayUrlBox => ObsView.ObsOverlayUrlBox;
    private WpfTextBox ObsOverlayWidthBox => ObsView.ObsOverlayWidthBox;
    private WpfTextBox ObsOverlayHeightBox => ObsView.ObsOverlayHeightBox;
    private WpfTextBox ObsOverlayMediaWidthBox => ObsView.ObsOverlayMediaWidthBox;
    private WpfTextBox ObsOverlayMediaHeightBox => ObsView.ObsOverlayMediaHeightBox;
    private WpfComboBox ObsOverlayPositionBox => ObsView.ObsOverlayPositionBox;
    private WpfTextBox ObsOverlayXBox => ObsView.ObsOverlayXBox;
    private WpfTextBox ObsOverlayYBox => ObsView.ObsOverlayYBox;
}
