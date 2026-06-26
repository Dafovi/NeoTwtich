using NeoTwitch.Services.Dashboard;
using NeoTwitch.Services.Status;
using NeoTwitch.Services.Ui;

namespace NeoTwitch;

public partial class MainWindow
{
    private void RefreshDashboardConnectionStates()
    {
        var states = DashboardConnectionStateService.Resolve(new DashboardConnectionStateInput(
            _isTwitchAuthorizing,
            _isTwitchConnecting,
            !string.IsNullOrWhiteSpace(_twitchConnectionError),
            _config.Token.HasToken,
            _config.ArduinoEnabled,
            _isArduinoConnecting,
            _lightController.HasConfirmedAck,
            _lightController.IsCompatibleWithoutAck,
            _lightController.HasOpenPort,
            _config.Alexa.Enabled,
            _isAlexaConnecting,
            _config.Alexa.IsConfigured,
            _alexaRelayConnected,
            _config.Obs.Enabled,
            _isObsConnecting,
            _obsService.IsConnected,
            !string.IsNullOrWhiteSpace(_obsConnectionError)));

        ConnectionVisualThemeService.ApplyDashboardState(
            DashboardTwitchStateText,
            DashboardTwitchStatusIcon,
            ConnectionStateService.GetVisual(states.Twitch, warningText: "Revisar"));
        ConnectionVisualThemeService.ApplyDashboardState(
            DashboardArduinoStateText,
            DashboardArduinoStatusIcon,
            ConnectionStateService.GetVisual(states.Arduino, warningText: "Sin respuesta"));
        ConnectionVisualThemeService.ApplyDashboardState(
            DashboardAlexaStateText,
            DashboardAlexaStatusIcon,
            ConnectionStateService.GetVisual(states.Alexa, warningText: _config.Alexa.IsConfigured ? "Configurado" : "Incompleta"));
        ConnectionVisualThemeService.ApplyDashboardState(
            DashboardObsStateText,
            DashboardObsStatusIcon,
            ConnectionStateService.GetVisual(states.Obs, warningText: "Revisar"));

        ConnectionVisualThemeService.ApplyConnectionBadge(
            ConnectionsTwitchBadge,
            ConnectionsTwitchBadgeText,
            ConnectionStateService.GetVisual(states.Twitch, warningText: "Revisar"));
        ConnectionVisualThemeService.ApplyConnectionBadge(
            ConnectionsArduinoBadge,
            ConnectionsArduinoBadgeText,
            ConnectionStateService.GetVisual(states.Arduino, warningText: "Sin respuesta"));
        ConnectionVisualThemeService.ApplyConnectionBadge(
            ConnectionsAlexaBadge,
            ConnectionsAlexaBadgeText,
            ConnectionStateService.GetVisual(states.Alexa, warningText: _config.Alexa.IsConfigured ? "Configurado" : "Incompleta"));
        ConnectionVisualThemeService.ApplyConnectionBadge(
            ConnectionsObsBadge,
            ConnectionsObsBadgeText,
            ConnectionStateService.GetVisual(states.Obs, warningText: "Revisar"));
    }
}
