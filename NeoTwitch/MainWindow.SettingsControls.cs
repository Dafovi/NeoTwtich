using System.Windows;
using System.Windows.Controls;
using WpfBorder = System.Windows.Controls.Border;
using WpfCheckBox = System.Windows.Controls.CheckBox;
using WpfComboBox = System.Windows.Controls.ComboBox;
using WpfRadioButton = System.Windows.Controls.RadioButton;
using WpfTextBox = System.Windows.Controls.TextBox;

namespace NeoTwitch;

public partial class MainWindow
{
    private WpfCheckBox StartHiddenCheck => SettingsView.StartHiddenCheck;
    private WpfCheckBox StartWithWindowsCheck => SettingsView.StartWithWindowsCheck;
    private WpfComboBox ThemeModeBox => SettingsView.ThemeModeBox;
    private WpfCheckBox CloseToTrayCheck => SettingsView.CloseToTrayCheck;
    private WpfBorder CloseToTrayCard => SettingsView.CloseToTrayCard;
    private WpfRadioButton CloseToTrayRadio => SettingsView.CloseToTrayRadio;
    private WpfBorder CloseAppCard => SettingsView.CloseAppCard;
    private WpfRadioButton CloseAppRadio => SettingsView.CloseAppRadio;
    private WpfTextBox MaxQueuedSameRuleAlertsBox => SettingsView.MaxQueuedSameRuleAlertsBox;
    private WpfTextBox SameRuleQueueCooldownBox => SettingsView.SameRuleQueueCooldownBox;
    private WpfTextBox MaxQueuedDifferentRuleAlertsBox => SettingsView.MaxQueuedDifferentRuleAlertsBox;
    private WpfTextBox DifferentRuleQueueCooldownBox => SettingsView.DifferentRuleQueueCooldownBox;
    private WpfCheckBox AutoTwitchCheck => SettingsView.AutoTwitchCheck;
    private WpfCheckBox AutoArduinoCheck => SettingsView.AutoArduinoCheck;
    private WpfCheckBox ObsAutoReconnectCheck => SettingsView.ObsAutoReconnectCheck;

    internal void GlobalSettingsChanged(object sender, RoutedEventArgs e)
    {
        if (_initializingComponent || _loadingUi)
        {
            return;
        }

        ApplyGlobalSettingsChange();
    }

    private void SelectCloseBehavior(object? parameter)
    {
        if (_initializingComponent || _loadingUi)
        {
            return;
        }

        _settingsViewModel.CloseToTray = string.Equals(parameter?.ToString(), "Tray", StringComparison.OrdinalIgnoreCase);
        ApplyGlobalSettingsChange();
    }

    private void ApplyGlobalSettingsChange()
    {
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
