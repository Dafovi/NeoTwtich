using System.Windows;
using System.Windows.Controls;
using WpfBorder = System.Windows.Controls.Border;
using WpfRadioButton = System.Windows.Controls.RadioButton;

namespace NeoTwitch;

public partial class MainWindow
{
    private WpfBorder CloseToTrayCard => SettingsView.CloseToTrayCard;
    private WpfRadioButton CloseToTrayRadio => SettingsView.CloseToTrayRadio;
    private WpfBorder CloseAppCard => SettingsView.CloseAppCard;
    private WpfRadioButton CloseAppRadio => SettingsView.CloseAppRadio;

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
