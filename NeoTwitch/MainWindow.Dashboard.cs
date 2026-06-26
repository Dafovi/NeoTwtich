using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using NeoTwitch.Models;
using NeoTwitch.Services.Dashboard;
using NeoTwitch.Services.Status;
using NeoTwitch.Services.Ui;
using NeoTwitch.ViewModels.Status;
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
        var twitchState = ConnectionStateService.ResolveTwitch(
            _isTwitchAuthorizing,
            _isTwitchConnecting,
            !string.IsNullOrWhiteSpace(_twitchConnectionError),
            _config.Token.HasToken);
        var arduinoState = ConnectionStateService.ResolveArduino(
            _config.ArduinoEnabled,
            _isArduinoConnecting,
            _lightController.HasConfirmedAck,
            _lightController.IsCompatibleWithoutAck,
            _lightController.HasOpenPort);
        var alexaState = ConnectionStateService.ResolveAlexa(
            _config.Alexa.Enabled,
            _isAlexaConnecting,
            _config.Alexa.IsConfigured,
            _alexaRelayConnected);
        var obsState = ConnectionStateService.ResolveObs(
            _config.Obs.Enabled,
            _isObsConnecting,
            _obsService.IsConnected,
            !string.IsNullOrWhiteSpace(_obsConnectionError));

        SetDashboardConnectionState(
            DashboardTwitchStateText,
            DashboardTwitchStatusIcon,
            twitchState,
            warningText: "Revisar");
        SetDashboardConnectionState(
            DashboardArduinoStateText,
            DashboardArduinoStatusIcon,
            arduinoState,
            warningText: "Sin respuesta");
        SetDashboardConnectionState(
            DashboardAlexaStateText,
            DashboardAlexaStatusIcon,
            alexaState,
            warningText: _config.Alexa.IsConfigured ? "Configurado" : "Incompleta");
        SetDashboardConnectionState(
            DashboardObsStateText,
            DashboardObsStatusIcon,
            obsState,
            warningText: "Revisar");

        SetConnectionBadgeState(
            ConnectionsTwitchBadge,
            ConnectionsTwitchBadgeText,
            twitchState,
            warningText: "Revisar");
        SetConnectionBadgeState(
            ConnectionsArduinoBadge,
            ConnectionsArduinoBadgeText,
            arduinoState,
            warningText: "Sin respuesta");
        SetConnectionBadgeState(
            ConnectionsAlexaBadge,
            ConnectionsAlexaBadgeText,
            alexaState,
            warningText: _config.Alexa.IsConfigured ? "Configurado" : "Incompleta");
        SetConnectionBadgeState(
            ConnectionsObsBadge,
            ConnectionsObsBadgeText,
            obsState,
            warningText: "Revisar");
    }

    private static void SetDashboardConnectionState(
        TextBlock stateText,
        Border statusIcon,
        ConnectionVisualState state,
        string connectedText = "Conectado",
        string disconnectedText = "Desconectado",
        string disabledText = "Desactivado",
        string connectingText = "Conectando",
        string warningText = "Revisar")
    {
        var (text, color, icon) = ConnectionStateService.GetVisual(
            state,
            connectedText,
            disconnectedText,
            disabledText,
            connectingText,
            warningText);
        var brush = FrozenBrushFrom(color);

        stateText.Text = text;
        stateText.Foreground = brush;
        statusIcon.Background = brush;
        statusIcon.OpacityMask = new ImageBrush
        {
            ImageSource = PackImageLoader.Load(icon),
            Stretch = Stretch.Uniform
        };
        statusIcon.ToolTip = text;
    }

    private static void SetConnectionBadgeState(
        Border badge,
        TextBlock textBlock,
        ConnectionVisualState state,
        string connectedText = "Conectado",
        string disconnectedText = "Desconectado",
        string disabledText = "Desactivado",
        string connectingText = "Conectando",
        string warningText = "Revisar")
    {
        var (text, color, _) = ConnectionStateService.GetVisual(
            state,
            connectedText,
            disconnectedText,
            disabledText,
            connectingText,
            warningText);
        var brush = FrozenBrushFrom(color);

        textBlock.Text = text;
        textBlock.Foreground = brush;
        badge.Background = TranslucentBrushFrom(color);
        badge.BorderBrush = brush;
        badge.BorderThickness = new Thickness(1);
    }

}
