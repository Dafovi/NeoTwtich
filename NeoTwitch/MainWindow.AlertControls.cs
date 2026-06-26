using WpfButton = System.Windows.Controls.Button;
using WpfCheckBox = System.Windows.Controls.CheckBox;
using WpfComboBox = System.Windows.Controls.ComboBox;
using WpfListBox = System.Windows.Controls.ListBox;
using WpfStackPanel = System.Windows.Controls.StackPanel;
using WpfTextBlock = System.Windows.Controls.TextBlock;
using WpfTextBox = System.Windows.Controls.TextBox;
using WpfToggleButton = System.Windows.Controls.Primitives.ToggleButton;

namespace NeoTwitch;

public partial class MainWindow
{
    private WpfTextBox RuleSearchBox => AlertsView.RuleSearchBox;
    private WpfToggleButton RuleFilterAllButton => AlertsView.RuleFilterAllButton;
    private WpfToggleButton RuleFilterActiveButton => AlertsView.RuleFilterActiveButton;
    private WpfToggleButton RuleFilterInactiveButton => AlertsView.RuleFilterInactiveButton;
    private WpfComboBox RuleCategoryFilterBox => AlertsView.RuleCategoryFilterBox;
    private WpfTextBlock RulesCountText => AlertsView.RulesCountText;
    private WpfListBox RulesList => AlertsView.RulesList;
    private WpfButton RuleTestButton => AlertsView.RuleTestButton;
    private WpfButton SaveRuleButton => AlertsView.SaveRuleButton;
    private WpfStackPanel RuleEditorPanel => AlertsView.RuleEditorPanel;
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
