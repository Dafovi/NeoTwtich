using WpfButton = System.Windows.Controls.Button;
using WpfComboBox = System.Windows.Controls.ComboBox;
using WpfTextBox = System.Windows.Controls.TextBox;

namespace NeoTwitch;

public partial class MainWindow
{
    private WpfTextBox AudioSearchBox => AudioView.AudioSearchBox;
    private WpfButton AudioFilterAllButton => AudioView.AudioFilterAllButton;
    private WpfButton AudioFilterWithAlertButton => AudioView.AudioFilterWithAlertButton;
    private WpfButton AudioFilterNoGroupButton => AudioView.AudioFilterNoGroupButton;
    private WpfComboBox NewAudioAlertBox => AudioView.NewAudioAlertBox;
    private WpfComboBox NewAudioGroupBox => AudioView.NewAudioGroupBox;
}
