using WpfButton = System.Windows.Controls.Button;
using WpfComboBox = System.Windows.Controls.ComboBox;
using WpfItemsControl = System.Windows.Controls.ItemsControl;
using WpfTextBlock = System.Windows.Controls.TextBlock;
using WpfTextBox = System.Windows.Controls.TextBox;

namespace NeoTwitch;

public partial class MainWindow
{
    private WpfTextBlock VideoSavedCountText => VideosView.VideoSavedCountText;
    private WpfTextBlock VideoGroupCountText => VideosView.VideoGroupCountText;
    private WpfTextBlock LastVideoText => VideosView.LastVideoText;
    private WpfTextBox VideoSearchBox => VideosView.VideoSearchBox;
    private WpfButton VideoFilterAllButton => VideosView.VideoFilterAllButton;
    private WpfButton VideoFilterWithGroupButton => VideosView.VideoFilterWithGroupButton;
    private WpfButton VideoFilterNoGroupButton => VideosView.VideoFilterNoGroupButton;
    private WpfItemsControl VideoLibraryList => VideosView.VideoLibraryList;
    private WpfTextBlock VideoLibraryFooterText => VideosView.VideoLibraryFooterText;
    private WpfTextBox NewVideoPathBox => VideosView.NewVideoPathBox;
    private WpfTextBox NewVideoNameBox => VideosView.NewVideoNameBox;
    private WpfComboBox NewVideoGroupBox => VideosView.NewVideoGroupBox;
    private WpfTextBox NewVideoGroupNameBox => VideosView.NewVideoGroupNameBox;
    private WpfItemsControl VideoGroupsList => VideosView.VideoGroupsList;
}
