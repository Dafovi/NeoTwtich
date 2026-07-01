using WpfButton = System.Windows.Controls.Button;
using WpfComboBox = System.Windows.Controls.ComboBox;
using WpfTextBox = System.Windows.Controls.TextBox;

namespace NeoTwitch;

public partial class MainWindow
{
    private WpfTextBox VideoSearchBox => VideosView.VideoSearchBox;
    private WpfButton VideoFilterAllButton => VideosView.VideoFilterAllButton;
    private WpfButton VideoFilterWithGroupButton => VideosView.VideoFilterWithGroupButton;
    private WpfButton VideoFilterNoGroupButton => VideosView.VideoFilterNoGroupButton;
    private WpfComboBox NewVideoGroupBox => VideosView.NewVideoGroupBox;
}
