using WpfButton = System.Windows.Controls.Button;
using WpfCheckBox = System.Windows.Controls.CheckBox;
using WpfComboBox = System.Windows.Controls.ComboBox;
using WpfListBox = System.Windows.Controls.ListBox;
using WpfTextBlock = System.Windows.Controls.TextBlock;
using WpfTextBox = System.Windows.Controls.TextBox;

namespace NeoTwitch;

public partial class MainWindow
{
    private WpfListBox RulesList => AlertsView.RulesList;
    private WpfButton RuleTestButton => AlertsView.RuleTestButton;
    private WpfTextBox RuleNameBox => AlertsView.RuleNameBox;
    private WpfCheckBox RuleEnabledCheck => AlertsView.RuleEnabledCheck;
    private WpfComboBox EventKindBox => AlertsView.EventKindBox;
    private WpfButton EventFollowTileButton => AlertsView.EventFollowTileButton;
    private WpfButton EventSubscriptionTileButton => AlertsView.EventSubscriptionTileButton;
    private WpfButton EventRaidTileButton => AlertsView.EventRaidTileButton;
    private WpfButton EventCheerTileButton => AlertsView.EventCheerTileButton;
    private WpfButton EventChatCommandTileButton => AlertsView.EventChatCommandTileButton;
    private WpfButton EventRedemptionTileButton => AlertsView.EventRedemptionTileButton;
    private WpfTextBlock RewardTitleLabel => AlertsView.RewardTitleLabel;
    private WpfTextBox RewardTitleBox => AlertsView.RewardTitleBox;
    private WpfTextBlock MinimumBitsLabel => AlertsView.MinimumBitsLabel;
    private WpfTextBox MinimumBitsBox => AlertsView.MinimumBitsBox;
    private WpfTextBlock ChatCommandLabel => AlertsView.ChatCommandLabel;
    private WpfTextBox ChatCommandBox => AlertsView.ChatCommandBox;
}
