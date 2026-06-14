using WpfButton = System.Windows.Controls.Button;
using WpfCheckBox = System.Windows.Controls.CheckBox;
using WpfGrid = System.Windows.Controls.Grid;
using WpfTextBlock = System.Windows.Controls.TextBlock;
using WpfTextBox = System.Windows.Controls.TextBox;

namespace NeoTwitch;

public partial class MainWindow
{
    private WpfTextBlock AlexaBackgroundUnavailableText => AlexaView.AlexaBackgroundUnavailableText;
    private WpfCheckBox BackgroundAlexaEnabledCheck => AlexaView.BackgroundAlexaEnabledCheck;
    private WpfCheckBox BackgroundAlexaTurnOffAfterEventCheck => AlexaView.BackgroundAlexaTurnOffAfterEventCheck;
    private WpfGrid BackgroundAlexaEventsGrid => AlexaView.BackgroundAlexaEventsGrid;
    private WpfTextBox BackgroundAlexaOnEventBox => AlexaView.BackgroundAlexaOnEventBox;
    private WpfTextBox BackgroundAlexaOffEventBox => AlexaView.BackgroundAlexaOffEventBox;
    private WpfButton ApplyAlexaBackgroundButton => AlexaView.ApplyAlexaBackgroundButton;
    private WpfButton StopAlexaBackgroundButton => AlexaView.StopAlexaBackgroundButton;
}
