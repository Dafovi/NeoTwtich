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
    private WpfComboBox NewImageGroupBox => ImagesView.NewImageGroupBox;
}
