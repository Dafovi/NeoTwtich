using WpfButton = System.Windows.Controls.Button;
using WpfComboBox = System.Windows.Controls.ComboBox;
using WpfItemsControl = System.Windows.Controls.ItemsControl;
using WpfTextBlock = System.Windows.Controls.TextBlock;
using WpfTextBox = System.Windows.Controls.TextBox;

namespace NeoTwitch;

public partial class MainWindow
{
    private WpfTextBlock ImageSavedCountText => ImagesView.ImageSavedCountText;
    private WpfTextBlock ImageGroupCountText => ImagesView.ImageGroupCountText;
    private WpfTextBlock LastImageText => ImagesView.LastImageText;
    private WpfTextBox ImageSearchBox => ImagesView.ImageSearchBox;
    private WpfButton ImageFilterAllButton => ImagesView.ImageFilterAllButton;
    private WpfButton ImageFilterWithGroupButton => ImagesView.ImageFilterWithGroupButton;
    private WpfButton ImageFilterNoGroupButton => ImagesView.ImageFilterNoGroupButton;
    private WpfItemsControl ImageLibraryList => ImagesView.ImageLibraryList;
    private WpfTextBlock ImageLibraryFooterText => ImagesView.ImageLibraryFooterText;
    private WpfTextBox NewImagePathBox => ImagesView.NewImagePathBox;
    private WpfTextBox NewImageNameBox => ImagesView.NewImageNameBox;
    private WpfComboBox NewImageGroupBox => ImagesView.NewImageGroupBox;
    private WpfTextBox NewImageGroupNameBox => ImagesView.NewImageGroupNameBox;
    private WpfItemsControl ImageGroupsList => ImagesView.ImageGroupsList;
}
