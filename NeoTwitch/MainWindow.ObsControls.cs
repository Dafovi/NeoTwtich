using WpfButton = System.Windows.Controls.Button;
using WpfListBox = System.Windows.Controls.ListBox;
using WpfTextBlock = System.Windows.Controls.TextBlock;

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
    private WpfButton TestObsButtonPanel => ObsView.TestObsButtonPanel;
    private WpfButton ConnectObsButtonPanel => ObsView.ConnectObsButtonPanel;
    private WpfListBox ObsScenesList => ObsView.ObsScenesList;
}
