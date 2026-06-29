using WpfButton = System.Windows.Controls.Button;
using WpfComboBox = System.Windows.Controls.ComboBox;
using WpfSlider = System.Windows.Controls.Slider;
using WpfTextBlock = System.Windows.Controls.TextBlock;
using WpfTextBox = System.Windows.Controls.TextBox;

namespace NeoTwitch;

public partial class MainWindow
{
    private WpfSlider AlertVolumeSlider => AudioView.AlertVolumeSlider;
    private WpfTextBlock AlertVolumeValueText => AudioView.AlertVolumeValueText;
    private WpfTextBox AudioSearchBox => AudioView.AudioSearchBox;
    private WpfButton AudioFilterAllButton => AudioView.AudioFilterAllButton;
    private WpfButton AudioFilterWithAlertButton => AudioView.AudioFilterWithAlertButton;
    private WpfButton AudioFilterNoGroupButton => AudioView.AudioFilterNoGroupButton;
    private WpfTextBox NewAudioPathBox => AudioView.NewAudioPathBox;
    private WpfTextBox NewAudioNameBox => AudioView.NewAudioNameBox;
    private WpfComboBox NewAudioAlertBox => AudioView.NewAudioAlertBox;
    private WpfComboBox NewAudioGroupBox => AudioView.NewAudioGroupBox;
    private WpfTextBox NewAudioGroupNameBox => AudioView.NewAudioGroupNameBox;
}
