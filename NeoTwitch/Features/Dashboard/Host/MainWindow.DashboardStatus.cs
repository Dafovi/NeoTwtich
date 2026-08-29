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

        _shellViewModel.UpdateChannel(channel.Name, channel.Login);
        _shellViewModel.UpdateServiceStatusText(
            twitchConnection: DashboardStatusTextService.BuildTwitchConnectionText(
                _isTwitchAuthorizing,
                _isTwitchConnecting,
                !string.IsNullOrWhiteSpace(_twitchConnectionError),
                _eventSubClient.IsHealthy,
                _config.Token.HasToken,
                labels),
            twitchStatus: DashboardStatusTextService.BuildTwitchStatusText(
                _isTwitchAuthorizing,
                _isTwitchConnecting,
                _streamStatus,
                _eventSubClient.IsHealthy,
                labels));
        UpdateTwitchLiveIndicator();
        UpdateChannelAvatar();

        var totalLeds = _config.LedStrips.Sum(strip => strip.LedCount);
        _shellViewModel.UpdateServiceStatusText(
            arduinoConnection: DashboardStatusTextService.BuildArduinoConnectionText(
                _config.ArduinoEnabled,
                _isArduinoConnecting,
                _lightController.HasConfirmedAck,
                _lightController.IsCompatibleWithoutAck,
                _lightController.HasOpenPort,
                _lightController.CurrentPort,
                labels),
            arduinoStatus: DashboardStatusTextService.BuildArduinoStatusText(
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
                labels));
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

        _lightsViewModel.UpdateArduinoStatus(status);
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
        _shellViewModel.UpdateServiceStatusText(
            alexaConnection: DashboardStatusTextService.BuildAlexaConnectionText(
                _config.Alexa.Enabled,
                _config.Alexa.IsConfigured,
                _isAlexaConnecting,
                _alexaRelayConnected,
                labels),
            alexaSidebarStatus: _config.Alexa.IsConfigured
            ? DashboardStatusTextService.BuildAlexaSidebarStatusText(
                _config.BackgroundAlexaEnabled,
                _config.BackgroundAlexaOnEventName,
                _config.BackgroundAlexaTurnOffAfterEvent,
                _config.BackgroundAlexaOffEventName,
                labels)
            : status);
        UpdateConnectionButtons();
        RefreshDashboardConnectionStates();
        UpdateDashboardSummary();
    }

    private DashboardStatusTextLabels GetDashboardStatusTextLabels()
    {
        return DashboardStatusTextLabelFactory.Build(_text);
    }
}
