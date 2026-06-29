using WpfButton = System.Windows.Controls.Button;
using WpfComboBox = System.Windows.Controls.ComboBox;
using WpfTextBox = System.Windows.Controls.TextBox;

namespace NeoTwitch;

public partial class MainWindow
{
    private WpfTextBox ImageSearchBox => ImagesView.ImageSearchBox;
    private WpfButton ImageFilterAllButton => ImagesView.ImageFilterAllButton;
    private WpfButton ImageFilterWithGroupButton => ImagesView.ImageFilterWithGroupButton;
    private WpfButton ImageFilterNoGroupButton => ImagesView.ImageFilterNoGroupButton;
    private WpfTextBox NewImagePathBox => ImagesView.NewImagePathBox;
    private WpfTextBox NewImageNameBox => ImagesView.NewImageNameBox;
    private WpfComboBox NewImageGroupBox => ImagesView.NewImageGroupBox;
    private WpfTextBox NewImageGroupNameBox => ImagesView.NewImageGroupNameBox;
}
