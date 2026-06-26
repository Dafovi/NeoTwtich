using NeoTwitch.Services.Dashboard;

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

        var channel = DashboardStatusTextService.BuildChannelDisplayText(
            _config.Channel.IsReady,
            _config.Channel.DisplayName,
            _config.Channel.Login);

        ChannelNameText.Text = channel.Name;
        ChannelLoginText.Text = channel.Login;
        TwitchConnectionText.Text = DashboardStatusTextService.BuildTwitchConnectionText(
            _isTwitchAuthorizing,
            _isTwitchConnecting,
            !string.IsNullOrWhiteSpace(_twitchConnectionError),
            _eventSubClient.IsRunning,
            _config.Token.HasToken);
        TwitchStatusText.Text = DashboardStatusTextService.BuildTwitchStatusText(
            _isTwitchAuthorizing,
            _isTwitchConnecting,
            _streamStatus,
            _eventSubClient.IsRunning);
        UpdateTwitchLiveIndicator();
        UpdateChannelAvatar();

        var totalLeds = _config.LedStrips.Sum(strip => strip.LedCount);
        ArduinoConnectionText.Text = DashboardStatusTextService.BuildArduinoConnectionText(
            _config.ArduinoEnabled,
            _isArduinoConnecting,
            _lightController.HasConfirmedAck,
            _lightController.IsCompatibleWithoutAck,
            _lightController.HasOpenPort,
            _lightController.CurrentPort);
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
            _config.BackgroundPattern);
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
            _config.LedStrips);

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

        var status = DashboardStatusTextService.BuildAlexaStatusText(_config.Alexa.Enabled, _config.Alexa.IsConfigured);

        AlexaStatusText.Text = status;
        AlexaConnectionText.Text = DashboardStatusTextService.BuildAlexaConnectionText(
            _config.Alexa.Enabled,
            _config.Alexa.IsConfigured,
            _isAlexaConnecting,
            _alexaRelayConnected);
        AlexaSidebarStatusText.Text = _config.Alexa.IsConfigured
            ? DashboardStatusTextService.BuildAlexaSidebarStatusText(
                _config.BackgroundAlexaEnabled,
                _config.BackgroundAlexaOnEventName,
                _config.BackgroundAlexaTurnOffAfterEvent,
                _config.BackgroundAlexaOffEventName)
            : status;
        UpdateConnectionButtons();
        RefreshDashboardConnectionStates();
        UpdateDashboardSummary();
    }
}
