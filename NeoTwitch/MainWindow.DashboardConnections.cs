using NeoTwitch.Services.Dashboard;
using NeoTwitch.Services.Status;
using NeoTwitch.Services.Text;

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

        var reviewLabels = GetConnectionStateLabels(UiTextKeys.ConnectionWarningReview);
        var arduinoLabels = GetConnectionStateLabels(UiTextKeys.ConnectionWarningNoResponse);
        var alexaLabels = GetConnectionStateLabels(_config.Alexa.IsConfigured
            ? UiTextKeys.ConnectionWarningConfigured
            : UiTextKeys.ConnectionWarningIncomplete);

        var twitchVisual = ConnectionStateService.GetVisual(states.Twitch, reviewLabels);
        var arduinoVisual = ConnectionStateService.GetVisual(states.Arduino, arduinoLabels);
        var alexaVisual = ConnectionStateService.GetVisual(states.Alexa, alexaLabels);
        var obsVisual = ConnectionStateService.GetVisual(states.Obs, reviewLabels);

        _dashboardViewModel.UpdateConnectionStates(
            twitchVisual,
            arduinoVisual,
            alexaVisual,
            obsVisual);
        _connectionsViewModel.UpdateBadges(
            twitchVisual,
            arduinoVisual,
            alexaVisual,
            obsVisual);
    }

    private ConnectionStateLabels GetConnectionStateLabels(string warningKey)
    {
        return new ConnectionStateLabels(
            _text.Get(UiTextKeys.ConnectionConnected),
            _text.Get(UiTextKeys.ConnectionDisconnected),
            _text.Get(UiTextKeys.ConnectionDisabled),
            _text.Get(UiTextKeys.ConnectionConnecting),
            _text.Get(warningKey));
    }
}
