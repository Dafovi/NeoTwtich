using WpfButton = System.Windows.Controls.Button;
using WpfComboBox = System.Windows.Controls.ComboBox;
using WpfListBox = System.Windows.Controls.ListBox;
using WpfTextBlock = System.Windows.Controls.TextBlock;
using WpfTextBox = System.Windows.Controls.TextBox;

namespace NeoTwitch;

public partial class MainWindow
{
    private WpfTextBlock ObsConnectionStateText => ObsView.ObsConnectionStateText;
    private WpfTextBlock ObsCurrentSceneText => ObsView.ObsCurrentSceneText;
    private WpfTextBlock ObsSceneCountText => ObsView.ObsSceneCountText;
    private WpfTextBlock ObsStudioModeText => ObsView.ObsStudioModeText;
    private WpfTextBlock ObsHostSummaryText => ObsView.ObsHostSummaryText;
    private WpfTextBlock ObsPortSummaryText => ObsView.ObsPortSummaryText;
    private WpfTextBlock ObsVersionText => ObsView.ObsVersionText;
    private WpfTextBlock ObsStatusText => ObsView.ObsStatusText;
    private WpfTextBox ObsOverlayUrlBox => ObsView.ObsOverlayUrlBox;
    private WpfTextBox ObsOverlayWidthBox => ObsView.ObsOverlayWidthBox;
    private WpfTextBox ObsOverlayHeightBox => ObsView.ObsOverlayHeightBox;
    private WpfTextBox ObsOverlayMediaWidthBox => ObsView.ObsOverlayMediaWidthBox;
    private WpfTextBox ObsOverlayMediaHeightBox => ObsView.ObsOverlayMediaHeightBox;
    private WpfComboBox ObsOverlayPositionBox => ObsView.ObsOverlayPositionBox;
    private WpfTextBox ObsOverlayXBox => ObsView.ObsOverlayXBox;
    private WpfTextBox ObsOverlayYBox => ObsView.ObsOverlayYBox;
    private WpfListBox ObsScenesList => ObsView.ObsScenesList;
}
