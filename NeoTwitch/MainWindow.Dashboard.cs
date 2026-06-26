using NeoTwitch.Models;
using NeoTwitch.Services.Dashboard;
using NeoTwitch.Services.Status;
using NeoTwitch.Services.Ui;
using static NeoTwitch.Services.Ui.UiBrushFactory;

namespace NeoTwitch;

public partial class MainWindow
{
    private void RegisterDashboardMatchedRules(int count)
    {
        if (!Dispatcher.CheckAccess())
        {
            _ = Dispatcher.InvokeAsync(() => RegisterDashboardMatchedRules(count));
            return;
        }

        _dashboardSummary.RegisterMatchedRules(count);
        UpdateDashboardSummary();
    }

    private void RegisterDashboardTwitchEvent(TwitchEvent twitchEvent)
    {
        if (!Dispatcher.CheckAccess())
        {
            _ = Dispatcher.InvokeAsync(() => RegisterDashboardTwitchEvent(twitchEvent));
            return;
        }

        _dashboardSummary.RegisterTwitchEvent(twitchEvent);
        UpdateDashboardSummary();
    }

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

    private void UpdateDashboardSummary()
    {
        if (!Dispatcher.CheckAccess())
        {
            _ = Dispatcher.InvokeAsync(UpdateDashboardSummary);
            return;
        }

        var summary = _dashboardSummary.Snapshot;
        DashboardFollowersSummaryText.Text = $"+{summary.Followers}";
        DashboardSubsSummaryText.Text = $"+{summary.Subscriptions}";
        DashboardBitsSummaryText.Text = $"+{summary.Bits}";
        DashboardChatSummaryText.Text = summary.ChatMessages.ToString();
        DashboardEventsSummaryText.Text = summary.Events.ToString();

        DashboardFollowersSummaryText.Foreground = FrozenBrushFrom("#14B8A6");
        DashboardSubsSummaryText.Foreground = FrozenBrushFrom("#B56CFF");
        DashboardBitsSummaryText.Foreground = FrozenBrushFrom("#37C7F3");
        DashboardChatSummaryText.Foreground = FrozenBrushFrom("#22C55E");
        DashboardEventsSummaryText.Foreground = FrozenBrushFrom("#84CC16");

        RefreshDashboardConnectionStates();
    }

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
