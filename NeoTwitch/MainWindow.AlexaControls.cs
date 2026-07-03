using System.Windows;
using NeoTwitch.Models;
using NeoTwitch.Services;
using NeoTwitch.ViewModels.Activity;
using WpfButton = System.Windows.Controls.Button;
using WpfCheckBox = System.Windows.Controls.CheckBox;
using WpfGrid = System.Windows.Controls.Grid;
using WpfTextBlock = System.Windows.Controls.TextBlock;

namespace NeoTwitch;

public partial class MainWindow
{
    private WpfTextBlock AlexaBackgroundUnavailableText => AlexaView.AlexaBackgroundUnavailableText;
    private WpfCheckBox BackgroundAlexaEnabledCheck => AlexaView.BackgroundAlexaEnabledCheck;
    private WpfCheckBox BackgroundAlexaTurnOffAfterEventCheck => AlexaView.BackgroundAlexaTurnOffAfterEventCheck;
    private WpfGrid BackgroundAlexaEventsGrid => AlexaView.BackgroundAlexaEventsGrid;
    private WpfButton ApplyAlexaBackgroundButton => AlexaView.ApplyAlexaBackgroundButton;
    private WpfButton StopAlexaBackgroundButton => AlexaView.StopAlexaBackgroundButton;

    internal void AlexaSettingsChanged(object sender, RoutedEventArgs e)
    {
        if (_initializingComponent || _loadingUi)
        {
            return;
        }

        SaveGlobalSettingsFromFields();
        _alexaRelayConnected = false;
        SaveConfig();
        UpdateAlexaStatusText();
        UpdateSensitiveFieldVisibility();
        RefreshRulesView();
        UpdateRuleOptionVisibility();
        UpdateNavigationButtons();
    }

    private async void TestAlexaConnection()
    {
        if (_isAlexaConnecting)
        {
            return;
        }

        try
        {
            _isAlexaConnecting = true;
            UpdateStatusText();
            SaveGlobalSettingsFromFields();
            SaveConfig();
            await _alexaRelayService.SendTestEventAsync(_config, CancellationToken.None);
            _alexaRelayConnected = true;
            AddLog("Alexa: evento de prueba enviado.", ActivityLogKind.Alexa);
        }
        catch (Exception ex)
        {
            _alexaRelayConnected = false;
            CrashReporter.Log(ex, "No se pudo enviar la prueba de Alexa.");
            AddLog($"Alexa: {ex.Message}", ActivityLogKind.Important);
            _dialog.ShowWarning("Alexa", ex.Message);
        }
        finally
        {
            _isAlexaConnecting = false;
            UpdateAlexaStatusText();
        }
    }

    private async void ApplyAlexaBackground()
    {
        SaveGlobalSettingsFromFields();
        SaveBackgroundFromFields();
        SaveConfig();

        if (_config.BackgroundAlexaEnabled)
        {
            await SendBackgroundAlexaEventAsync(_config.BackgroundAlexaOnEventName, "Fondo Alexa encendido", force: true);
        }
    }

    private async void StopAlexaBackground()
    {
        SaveGlobalSettingsFromFields();
        SaveBackgroundFromFields();
        SaveConfig();
        await SendBackgroundAlexaEventAsync(_config.BackgroundAlexaOffEventName, "Fondo Alexa apagado", force: true);
    }
}
