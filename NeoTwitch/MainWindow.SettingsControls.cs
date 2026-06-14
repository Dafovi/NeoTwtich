using WpfBorder = System.Windows.Controls.Border;
using WpfCheckBox = System.Windows.Controls.CheckBox;
using WpfComboBox = System.Windows.Controls.ComboBox;
using WpfImage = System.Windows.Controls.Image;
using WpfRadioButton = System.Windows.Controls.RadioButton;
using WpfTextBlock = System.Windows.Controls.TextBlock;
using WpfTextBox = System.Windows.Controls.TextBox;

namespace NeoTwitch;

public partial class MainWindow
{
    private WpfCheckBox StartHiddenCheck => SettingsView.StartHiddenCheck;
    private WpfCheckBox StartWithWindowsCheck => SettingsView.StartWithWindowsCheck;
    private WpfComboBox ThemeModeBox => SettingsView.ThemeModeBox;
    private WpfTextBlock SettingsDiagnosticStatusText => SettingsView.SettingsDiagnosticStatusText;
    private WpfCheckBox CloseToTrayCheck => SettingsView.CloseToTrayCheck;
    private WpfBorder CloseToTrayCard => SettingsView.CloseToTrayCard;
    private WpfRadioButton CloseToTrayRadio => SettingsView.CloseToTrayRadio;
    private WpfBorder CloseAppCard => SettingsView.CloseAppCard;
    private WpfRadioButton CloseAppRadio => SettingsView.CloseAppRadio;
    private WpfImage SettingsAppStateIcon => SettingsView.SettingsAppStateIcon;
    private WpfTextBlock SettingsVersionText => SettingsView.SettingsVersionText;
    private WpfTextBox MaxQueuedSameRuleAlertsBox => SettingsView.MaxQueuedSameRuleAlertsBox;
    private WpfTextBox SameRuleQueueCooldownBox => SettingsView.SameRuleQueueCooldownBox;
    private WpfTextBox MaxQueuedDifferentRuleAlertsBox => SettingsView.MaxQueuedDifferentRuleAlertsBox;
    private WpfTextBox DifferentRuleQueueCooldownBox => SettingsView.DifferentRuleQueueCooldownBox;
    private WpfCheckBox AutoTwitchCheck => SettingsView.AutoTwitchCheck;
    private WpfCheckBox AutoArduinoCheck => SettingsView.AutoArduinoCheck;
    private WpfTextBlock SettingsPathText => SettingsView.SettingsPathText;
    private WpfTextBlock BackupPathText => SettingsView.BackupPathText;
}
