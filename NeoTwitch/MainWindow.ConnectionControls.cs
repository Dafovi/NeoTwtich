using WpfButton = System.Windows.Controls.Button;
using WpfCheckBox = System.Windows.Controls.CheckBox;
using WpfComboBox = System.Windows.Controls.ComboBox;
using WpfTextBlock = System.Windows.Controls.TextBlock;
using WpfTextBox = System.Windows.Controls.TextBox;

namespace NeoTwitch;

public partial class MainWindow
{
    private WpfTextBox ClientIdBox => ConnectionsView.ClientIdBox;
    private WpfTextBlock ClientIdMaskText => ConnectionsView.ClientIdMaskText;
    private WpfButton ClientIdRevealButton => ConnectionsView.ClientIdRevealButton;
    private WpfTextBox ClientSecretBox => ConnectionsView.ClientSecretBox;
    private WpfTextBlock ClientSecretMaskText => ConnectionsView.ClientSecretMaskText;
    private WpfButton ClientSecretRevealButton => ConnectionsView.ClientSecretRevealButton;

    private WpfCheckBox ArduinoEnabledCheck => ConnectionsView.ArduinoEnabledCheck;
    private WpfComboBox PortComboBox => ConnectionsView.PortComboBox;
    private WpfTextBox BaudRateBox => ConnectionsView.BaudRateBox;

    private WpfCheckBox AlexaEnabledCheck => ConnectionsView.AlexaEnabledCheck;
    private WpfTextBox AlexaRelayUrlBox => ConnectionsView.AlexaRelayUrlBox;
    private WpfTextBlock AlexaRelayUrlMaskText => ConnectionsView.AlexaRelayUrlMaskText;
    private WpfButton AlexaRelayUrlRevealButton => ConnectionsView.AlexaRelayUrlRevealButton;
    private WpfTextBox AlexaAuthTokenBox => ConnectionsView.AlexaAuthTokenBox;
    private WpfTextBlock AlexaAuthTokenMaskText => ConnectionsView.AlexaAuthTokenMaskText;
    private WpfButton AlexaAuthTokenRevealButton => ConnectionsView.AlexaAuthTokenRevealButton;

    private WpfCheckBox ObsEnabledCheck => ConnectionsView.ObsEnabledCheck;
    private WpfTextBox ObsHostBox => ConnectionsView.ObsHostBox;
    private WpfTextBox ObsPortBox => ConnectionsView.ObsPortBox;
    private WpfTextBox ObsPasswordBox => ConnectionsView.ObsPasswordBox;
    private WpfTextBlock ObsPasswordMaskText => ConnectionsView.ObsPasswordMaskText;
    private WpfButton ObsPasswordRevealButton => ConnectionsView.ObsPasswordRevealButton;
}
