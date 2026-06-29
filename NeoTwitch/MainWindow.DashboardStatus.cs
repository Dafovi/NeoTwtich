using NeoTwitch.Services.Dashboard;
using NeoTwitch.Services.Text;

namespace NeoTwitch;

public partial class MainWindow
{
    private void UpdateStatusText()
    {
        if (!Dispatcher.CheckAccess())
        {
            _ = Dispatcher.InvokeAsync(UpdateStatusText);
            return;
        }

        var labels = GetDashboardStatusTextLabels();
        var channel = DashboardStatusTextService.BuildChannelDisplayText(
            _config.Channel.IsReady,
            _config.Channel.DisplayName,
            _config.Channel.Login,
            labels);

        ChannelNameText.Text = channel.Name;
        ChannelLoginText.Text = channel.Login;
        TwitchConnectionText.Text = DashboardStatusTextService.BuildTwitchConnectionText(
            _isTwitchAuthorizing,
            _isTwitchConnecting,
            !string.IsNullOrWhiteSpace(_twitchConnectionError),
            _eventSubClient.IsRunning,
            _config.Token.HasToken,
            labels);
        TwitchStatusText.Text = DashboardStatusTextService.BuildTwitchStatusText(
            _isTwitchAuthorizing,
            _isTwitchConnecting,
            _streamStatus,
            _eventSubClient.IsRunning,
            labels);
        UpdateTwitchLiveIndicator();
        UpdateChannelAvatar();

        var totalLeds = _config.LedStrips.Sum(strip => strip.LedCount);
        ArduinoConnectionText.Text = DashboardStatusTextService.BuildArduinoConnectionText(
            _config.ArduinoEnabled,
            _isArduinoConnecting,
            _lightController.HasConfirmedAck,
            _lightController.IsCompatibleWithoutAck,
            _lightController.HasOpenPort,
            _lightController.CurrentPort,
            labels);
        ArduinoStatusText.Text = DashboardStatusTextService.BuildArduinoStatusText(
            _config.ArduinoEnabled,
            _isArduinoConnecting,
            _lightController.HasConfirmedAck,
            _lightController.IsCompatibleWithoutAck,
            _lightController.HasOpenPort,
            _config.SerialPort,
            _config.BaudRate,
            _config.LedStrips.Count,
            totalLeds,
            _config.BackgroundEnabled,
            DisplayNameService.For(_config.BackgroundPattern, _text),
            labels);
        RefreshDashboardConnectionStates();
        UpdateDashboardSummary();
        UpdateLightsArduinoStatus();

        UpdateConnectionButtons();
    }

    private void UpdateLightsArduinoStatus()
    {
        if (!Dispatcher.CheckAccess())
        {
            _ = Dispatcher.InvokeAsync(UpdateLightsArduinoStatus);
            return;
        }

        if (_initializingComponent)
        {
            return;
        }

        var status = DashboardStatusTextService.BuildLightsArduinoStatusText(
            _config.ArduinoEnabled,
            _lightController.HasConfirmedAck,
            _lightController.IsCompatibleWithoutAck,
            _lightController.HasOpenPort,
            _lightController.CurrentPort,
            _config.SerialPort,
            _config.LedStrips,
            GetDashboardStatusTextLabels());

        LightsArduinoDeviceText.Text = status.Device;
        LightsArduinoPortText.Text = status.Port;
        LightsArduinoLedCountText.Text = status.LedCount;
        LightsArduinoPinsText.Text = status.Pins;
    }

    private void UpdateAlexaStatusText()
    {
        if (!Dispatcher.CheckAccess())
        {
            _ = Dispatcher.InvokeAsync(UpdateAlexaStatusText);
            return;
        }

        var labels = GetDashboardStatusTextLabels();
        var status = DashboardStatusTextService.BuildAlexaStatusText(_config.Alexa.Enabled, _config.Alexa.IsConfigured, labels);

        _connectionsViewModel.UpdateAlexaStatusText(status);
        AlexaConnectionText.Text = DashboardStatusTextService.BuildAlexaConnectionText(
            _config.Alexa.Enabled,
            _config.Alexa.IsConfigured,
            _isAlexaConnecting,
            _alexaRelayConnected,
            labels);
        AlexaSidebarStatusText.Text = _config.Alexa.IsConfigured
            ? DashboardStatusTextService.BuildAlexaSidebarStatusText(
                _config.BackgroundAlexaEnabled,
                _config.BackgroundAlexaOnEventName,
                _config.BackgroundAlexaTurnOffAfterEvent,
                _config.BackgroundAlexaOffEventName,
                labels)
            : status;
        UpdateConnectionButtons();
        RefreshDashboardConnectionStates();
        UpdateDashboardSummary();
    }

    private DashboardStatusTextLabels GetDashboardStatusTextLabels()
    {
        return new DashboardStatusTextLabels(
            _text.Get(UiTextKeys.DashboardNoTwitch),
            _text.Get(UiTextKeys.DashboardNoLogin),
            _text.Get(UiTextKeys.DashboardDefaultChannelName),
            _text.Get(UiTextKeys.DashboardTwitchAuthorizing),
            _text.Get(UiTextKeys.ConnectionConnecting),
            _text.Get(UiTextKeys.ObsReviewConnection),
            _text.Get(UiTextKeys.DashboardTwitchEventsConnected),
            _text.Get(UiTextKeys.DashboardTwitchSessionAuthorized),
            _text.Get(UiTextKeys.DashboardTwitchDisconnected),
            _text.Get(UiTextKeys.DashboardTwitchWaitingAuthorization),
            _text.Get(UiTextKeys.DashboardTwitchConnectingEvents),
            _text.Get(UiTextKeys.DashboardTwitchLiveWithGameFormat),
            _text.Get(UiTextKeys.DashboardTwitchLiveFormat),
            _text.Get(UiTextKeys.DashboardTwitchOffline),
            _text.Get(UiTextKeys.DashboardTwitchListeningUnqueried),
            _text.Get(UiTextKeys.DashboardTwitchReady),
            _text.Get(UiTextKeys.ConnectionDisabled),
            _text.Get(UiTextKeys.ConnectionConnecting),
            _text.Get(UiTextKeys.DashboardArduinoConnectedFormat),
            _text.Get(UiTextKeys.DashboardArduinoDefaultPort),
            _text.Get(UiTextKeys.DashboardArduinoVerifying),
            _text.Get(UiTextKeys.DashboardArduinoDisconnected),
            _text.Get(UiTextKeys.DashboardArduinoDisabledStatus),
            _text.Get(UiTextKeys.DashboardArduinoConnectingStatusFormat),
            _text.Get(UiTextKeys.DashboardArduinoConfiguredPortFallback),
            _text.Get(UiTextKeys.DashboardArduinoBackgroundFormat),
            _text.Get(UiTextKeys.DashboardArduinoBackgroundOff),
            _text.Get(UiTextKeys.DashboardArduinoAckStatusFormat),
            _text.Get(UiTextKeys.DashboardArduinoCompatibleStatusFormat),
            _text.Get(UiTextKeys.DashboardArduinoOpenPortStatus),
            _text.Get(UiTextKeys.DashboardArduinoPortSummaryFormat),
            _text.Get(UiTextKeys.DashboardArduinoNoCom),
            _text.Get(UiTextKeys.DashboardLightsNoPins),
            _text.Get(UiTextKeys.DashboardLightsPinFormat),
            _text.Get(UiTextKeys.DashboardLightsVerifying),
            _text.Get(UiTextKeys.ConnectionConnected),
            _text.Get(UiTextKeys.ConnectionDisconnected),
            _text.Get(UiTextKeys.ConnectionConnecting),
            _text.Get(UiTextKeys.DashboardAlexaReady),
            _text.Get(UiTextKeys.DashboardAlexaMissingUrl),
            _text.Get(UiTextKeys.DashboardAlexaDisabled),
            _text.Get(UiTextKeys.DashboardAlexaRelayConnected),
            _text.Get(UiTextKeys.DashboardAlexaRelayConfigured),
            _text.Get(UiTextKeys.DashboardAlexaIncomplete),
            _text.Get(UiTextKeys.DashboardAlexaBackgroundFormat),
            _text.Get(UiTextKeys.DashboardAlexaBackgroundOff),
            _text.Get(UiTextKeys.DashboardAlexaEndOffFormat),
            _text.Get(UiTextKeys.DashboardAlexaEndKeep),
            _text.Get(UiTextKeys.DashboardAlexaSidebarFormat));
    }
}
