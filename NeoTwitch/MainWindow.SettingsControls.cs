using System.Windows;
using System.Windows.Controls;
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
    private WpfCheckBox ObsAutoReconnectCheck => SettingsView.ObsAutoReconnectCheck;
    private WpfTextBlock SettingsPathText => SettingsView.SettingsPathText;
    private WpfTextBlock BackupPathText => SettingsView.BackupPathText;

    internal void GlobalSettingsChanged(object sender, RoutedEventArgs e)
    {
        if (_initializingComponent || _loadingUi)
        {
            return;
        }

        SaveGlobalSettingsFromFields();
        SaveConfig();
        ApplyStartWithWindowsRegistration();
        UpdateSensitiveFieldVisibility();
        UpdateSliderLabels();
        UpdateStatusText();
        RefreshRulesView();
        UpdateRuleOptionVisibility();
        ApplyBackgroundOutputMode();
        UpdateNavigationButtons();
        UpdateCloseBehaviorCards();
    }

    internal void CloseBehaviorRadio_Checked(object sender, RoutedEventArgs e)
    {
        if (_initializingComponent || _loadingUi)
        {
            return;
        }

        CloseToTrayCheck.IsChecked = sender == CloseToTrayRadio;
        GlobalSettingsChanged(sender, e);
    }

    internal void ThemeModeChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_initializingComponent || _loadingUi)
        {
            return;
        }

        SaveGlobalSettingsFromFields();
        ApplyTheme();
        SaveConfig();
        UpdateCloseBehaviorCards();
    }
}
