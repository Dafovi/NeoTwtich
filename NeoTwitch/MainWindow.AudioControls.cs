using WpfButton = System.Windows.Controls.Button;
using WpfComboBox = System.Windows.Controls.ComboBox;
using WpfItemsControl = System.Windows.Controls.ItemsControl;
using WpfSlider = System.Windows.Controls.Slider;
using WpfTextBlock = System.Windows.Controls.TextBlock;
using WpfTextBox = System.Windows.Controls.TextBox;

namespace NeoTwitch;

public partial class MainWindow
{
    private WpfSlider AlertVolumeSlider => AudioView.AlertVolumeSlider;
    private WpfTextBlock AlertVolumeValueText => AudioView.AlertVolumeValueText;
    private WpfTextBlock AudioSavedCountText => AudioView.AudioSavedCountText;
    private WpfTextBlock AudioGroupCountText => AudioView.AudioGroupCountText;
    private WpfTextBlock LastAudioText => AudioView.LastAudioText;
    private WpfTextBox AudioSearchBox => AudioView.AudioSearchBox;
    private WpfButton AudioFilterAllButton => AudioView.AudioFilterAllButton;
    private WpfButton AudioFilterWithAlertButton => AudioView.AudioFilterWithAlertButton;
    private WpfButton AudioFilterNoGroupButton => AudioView.AudioFilterNoGroupButton;
    private WpfItemsControl AudioLibraryList => AudioView.AudioLibraryList;
    private WpfTextBlock AudioLibraryFooterText => AudioView.AudioLibraryFooterText;
    private WpfTextBox NewAudioPathBox => AudioView.NewAudioPathBox;
    private WpfTextBox NewAudioNameBox => AudioView.NewAudioNameBox;
    private WpfComboBox NewAudioAlertBox => AudioView.NewAudioAlertBox;
    private WpfComboBox NewAudioGroupBox => AudioView.NewAudioGroupBox;
    private WpfTextBox NewAudioGroupNameBox => AudioView.NewAudioGroupNameBox;
    private WpfItemsControl AudioGroupsList => AudioView.AudioGroupsList;
}
