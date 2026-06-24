using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using NeoTwitch.Models;
using NeoTwitch.Services.Status;
using NeoTwitch.ViewModels.Status;

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

        _dashboardEventsToday += count;
        UpdateDashboardSummary();
    }

    private void RegisterDashboardTwitchEvent(TwitchEvent twitchEvent)
    {
        if (!Dispatcher.CheckAccess())
        {
            _ = Dispatcher.InvokeAsync(() => RegisterDashboardTwitchEvent(twitchEvent));
            return;
        }

        switch (twitchEvent.Kind)
        {
            case TwitchEventKind.Follow:
                _dashboardFollowersToday++;
                break;
            case TwitchEventKind.Subscription:
                _dashboardSubscriptionsToday++;
                break;
            case TwitchEventKind.Cheer:
                _dashboardBitsToday += Math.Max(0, twitchEvent.Bits ?? 0);
                break;
            case TwitchEventKind.ChatCommand:
                _dashboardChatMessagesToday++;
                break;
        }

        UpdateDashboardSummary();
    }

    private void UpdateStatusText()
    {
        if (!Dispatcher.CheckAccess())
        {
            _ = Dispatcher.InvokeAsync(UpdateStatusText);
            return;
        }

        var channelName = _config.Channel.IsReady
            ? FirstNonEmpty(_config.Channel.DisplayName, _config.Channel.Login, "Canal Twitch")
            : "Sin Twitch";
        var login = _config.Channel.IsReady && !string.IsNullOrWhiteSpace(_config.Channel.Login)
            ? $"@{_config.Channel.Login}"
            : "Sin login";

        ChannelNameText.Text = channelName;
        ChannelLoginText.Text = login;
        TwitchConnectionText.Text = _isTwitchAuthorizing
            ? "Autorizando"
            : _isTwitchConnecting
            ? "Conectando"
            : !string.IsNullOrWhiteSpace(_twitchConnectionError)
                ? "Revisar conexion"
                : _eventSubClient.IsRunning
                    ? "Eventos conectados"
                    : _config.Token.HasToken
                        ? "Sesion autorizada"
                        : "Sin conectar";
        TwitchStatusText.Text = BuildTwitchStatusText();
        UpdateTwitchLiveIndicator();
        UpdateChannelAvatar();

        var totalLeds = _config.LedStrips.Sum(strip => strip.LedCount);
        var activeBackground = _config.BackgroundEnabled
            ? $"{DisplayNames.For(_config.BackgroundPattern)} de fondo"
            : "Fondo apagado";
        ArduinoConnectionText.Text = !_config.ArduinoEnabled
            ? "Desactivado"
            : _isArduinoConnecting
                ? "Conectando"
            : _lightController.HasConfirmedAck || _lightController.IsCompatibleWithoutAck
                ? $"Conectado en {_lightController.CurrentPort}"
                : _lightController.HasOpenPort
                    ? "Verificando Arduino"
                : "Sin conectar";
        ArduinoStatusText.Text = !_config.ArduinoEnabled
            ? "Las luces Arduino no se mostraran ni ejecutaran."
            : _isArduinoConnecting
                ? $"Intentando conectar con {FirstNonEmpty(_config.SerialPort, "el puerto configurado")}."
            : _lightController.HasConfirmedAck
                ? $"{_config.BaudRate} baudios. {_config.LedStrips.Count} tiras, {totalLeds} LEDs. {activeBackground}."
                : _lightController.IsCompatibleWithoutAck
                    ? $"{_config.BaudRate} baudios. Modo compatible sin ACK; las luces pueden funcionar, pero el sketch no confirmo comandos."
                : _lightController.HasOpenPort
                    ? "El puerto esta abierto; esperando confirmacion del sketch."
                : $"Puerto: {FirstNonEmpty(_config.SerialPort, "sin COM")}. {_config.LedStrips.Count} tiras, {totalLeds} LEDs.";
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

        var totalLeds = _config.LedStrips.Sum(strip => strip.LedCount);
        var pins = _config.LedStrips.Count == 0
            ? "Sin pines"
            : string.Join(", ", _config.LedStrips.Select(strip => $"Pin {strip.Pin}"));

        LightsArduinoDeviceText.Text = !_config.ArduinoEnabled
            ? "Desactivado"
            : _lightController.HasConfirmedAck || _lightController.IsCompatibleWithoutAck
                ? "Conectado"
            : _lightController.HasOpenPort
                    ? "Verificando"
                    : "Desconectado";
        LightsArduinoPortText.Text = _lightController.HasOpenPort
            ? FirstNonEmpty(_lightController.CurrentPort, _config.SerialPort, "Sin COM")
            : FirstNonEmpty(_config.SerialPort, "Sin COM");
        LightsArduinoLedCountText.Text = totalLeds.ToString();
        LightsArduinoPinsText.Text = pins;
    }

    private void UpdateAlexaStatusText()
    {
        if (!Dispatcher.CheckAccess())
        {
            _ = Dispatcher.InvokeAsync(UpdateAlexaStatusText);
            return;
        }

        var status = _config.Alexa.IsConfigured
            ? "Alexa lista. Las reglas pueden enviar eventos a la Skill/relay."
            : _config.Alexa.Enabled
                ? "Alexa activa, falta configurar una URL valida de Skill/relay."
                : "Alexa desactivada. Las reglas no mostraran acciones de Alexa.";

        AlexaStatusText.Text = status;
        AlexaConnectionText.Text = _config.Alexa.IsConfigured
            ? _isAlexaConnecting
                ? "Conectando"
                : _alexaRelayConnected
                    ? "Relay conectado"
                    : "Relay configurado"
            : _config.Alexa.Enabled
                ? "Configuracion incompleta"
                : "Desactivado";
        AlexaSidebarStatusText.Text = _config.Alexa.IsConfigured
            ? BuildAlexaSidebarStatusText()
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

        DashboardFollowersSummaryText.Text = $"+{_dashboardFollowersToday}";
        DashboardSubsSummaryText.Text = $"+{_dashboardSubscriptionsToday}";
        DashboardBitsSummaryText.Text = $"+{_dashboardBitsToday}";
        DashboardChatSummaryText.Text = _dashboardChatMessagesToday.ToString();
        DashboardEventsSummaryText.Text = _dashboardEventsToday.ToString();

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
            ImageSource = LoadPackImage(icon),
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

    private static ImageSource? LoadPackImage(string path)
    {
        foreach (var uri in new[]
        {
            $"pack://application:,,,/NeoTwitch;component/{path}",
            $"pack://application:,,,/{path}"
        })
        {
            try
            {
                var image = new BitmapImage(new Uri(uri, UriKind.Absolute));
                image.Freeze();
                return image;
            }
            catch
            {
                // Some WPF resource contexts prefer the assembly-qualified URI, others the app-root URI.
            }
        }

        return null;
    }

    private string BuildTwitchStatusText()
    {
        if (_isTwitchAuthorizing)
        {
            return "Esperando autorizacion de Twitch.";
        }

        if (_isTwitchConnecting)
        {
            return "Conectando EventSub y chat de Twitch.";
        }

        if (_streamStatus is { IsLive: true } live)
        {
            var game = string.IsNullOrWhiteSpace(live.GameName)
                ? ""
                : $" en {live.GameName}";
            return $"En directo{game}. {live.ViewerCount} espectadores.";
        }

        if (_streamStatus is { IsLive: false })
        {
            return "Canal sin directo activo.";
        }

        return _eventSubClient.IsRunning
            ? "Escuchando eventos. Directo sin consultar."
            : "Listo para conectar eventos.";
    }
}
