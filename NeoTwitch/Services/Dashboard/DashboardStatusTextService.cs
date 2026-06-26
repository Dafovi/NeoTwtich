using NeoTwitch.Models;
using NeoTwitch.ViewModels.Status;

namespace NeoTwitch.Services.Dashboard;

public static class DashboardStatusTextService
{
    public static ChannelDisplayText BuildChannelDisplayText(bool channelReady, string? displayName, string? login)
    {
        if (!channelReady)
        {
            return new ChannelDisplayText("Sin Twitch", "Sin login");
        }

        var normalizedLogin = FirstNonEmpty(login, "");
        var channelName = FirstNonEmpty(displayName, normalizedLogin, "Canal Twitch");
        var loginText = string.IsNullOrWhiteSpace(normalizedLogin)
            ? "Sin login"
            : $"@{normalizedLogin}";

        return new ChannelDisplayText(channelName, loginText);
    }

    public static string BuildTwitchConnectionText(
        bool isAuthorizing,
        bool isConnecting,
        bool hasConnectionError,
        bool eventSubRunning,
        bool hasToken)
    {
        if (isAuthorizing)
        {
            return "Autorizando";
        }

        if (isConnecting)
        {
            return "Conectando";
        }

        if (hasConnectionError)
        {
            return "Revisar conexion";
        }

        if (eventSubRunning)
        {
            return "Eventos conectados";
        }

        return hasToken ? "Sesion autorizada" : "Sin conectar";
    }

    public static string BuildTwitchStatusText(
        bool isAuthorizing,
        bool isConnecting,
        TwitchStreamStatus? streamStatus,
        bool eventSubRunning)
    {
        if (isAuthorizing)
        {
            return "Esperando autorizacion de Twitch.";
        }

        if (isConnecting)
        {
            return "Conectando EventSub y chat de Twitch.";
        }

        if (streamStatus is { IsLive: true } live)
        {
            var game = string.IsNullOrWhiteSpace(live.GameName)
                ? ""
                : $" en {live.GameName}";
            return $"En directo{game}. {live.ViewerCount} espectadores.";
        }

        if (streamStatus is { IsLive: false })
        {
            return "Canal sin directo activo.";
        }

        return eventSubRunning
            ? "Escuchando eventos. Directo sin consultar."
            : "Listo para conectar eventos.";
    }

    public static string BuildArduinoConnectionText(
        bool arduinoEnabled,
        bool isConnecting,
        bool hasConfirmedAck,
        bool compatibleWithoutAck,
        bool hasOpenPort,
        string? currentPort)
    {
        if (!arduinoEnabled)
        {
            return "Desactivado";
        }

        if (isConnecting)
        {
            return "Conectando";
        }

        if (hasConfirmedAck || compatibleWithoutAck)
        {
            return $"Conectado en {FirstNonEmpty(currentPort, "COM")}";
        }

        return hasOpenPort ? "Verificando Arduino" : "Sin conectar";
    }

    public static string BuildArduinoStatusText(
        bool arduinoEnabled,
        bool isConnecting,
        bool hasConfirmedAck,
        bool compatibleWithoutAck,
        bool hasOpenPort,
        string? serialPort,
        int baudRate,
        int stripCount,
        int totalLeds,
        bool backgroundEnabled,
        LightPattern backgroundPattern)
    {
        if (!arduinoEnabled)
        {
            return "Las luces Arduino no se mostraran ni ejecutaran.";
        }

        if (isConnecting)
        {
            return $"Intentando conectar con {FirstNonEmpty(serialPort, "el puerto configurado")}.";
        }

        if (hasConfirmedAck)
        {
            var activeBackground = backgroundEnabled
                ? $"{DisplayNames.For(backgroundPattern)} de fondo"
                : "Fondo apagado";
            return $"{baudRate} baudios. {stripCount} tiras, {totalLeds} LEDs. {activeBackground}.";
        }

        if (compatibleWithoutAck)
        {
            return $"{baudRate} baudios. Modo compatible sin ACK; las luces pueden funcionar, pero el sketch no confirmo comandos.";
        }

        if (hasOpenPort)
        {
            return "El puerto esta abierto; esperando confirmacion del sketch.";
        }

        return $"Puerto: {FirstNonEmpty(serialPort, "sin COM")}. {stripCount} tiras, {totalLeds} LEDs.";
    }

    public static LightsArduinoStatusText BuildLightsArduinoStatusText(
        bool arduinoEnabled,
        bool hasConfirmedAck,
        bool compatibleWithoutAck,
        bool hasOpenPort,
        string? currentPort,
        string? configuredPort,
        IEnumerable<LedStripConfig> ledStrips)
    {
        var strips = ledStrips.ToList();
        var totalLeds = strips.Sum(strip => strip.LedCount);
        var pins = strips.Count == 0
            ? "Sin pines"
            : string.Join(", ", strips.Select(strip => $"Pin {strip.Pin}"));
        var device = !arduinoEnabled
            ? "Desactivado"
            : hasConfirmedAck || compatibleWithoutAck
                ? "Conectado"
                : hasOpenPort
                    ? "Verificando"
                    : "Desconectado";
        var port = hasOpenPort
            ? FirstNonEmpty(currentPort, configuredPort, "Sin COM")
            : FirstNonEmpty(configuredPort, "Sin COM");

        return new LightsArduinoStatusText(device, port, totalLeds.ToString(), pins);
    }

    public static string BuildAlexaStatusText(bool enabled, bool isConfigured)
    {
        return isConfigured
            ? "Alexa lista. Las reglas pueden enviar eventos a la Skill/relay."
            : enabled
                ? "Alexa activa, falta configurar una URL valida de Skill/relay."
                : "Alexa desactivada. Las reglas no mostraran acciones de Alexa.";
    }

    public static string BuildAlexaConnectionText(bool enabled, bool isConfigured, bool isConnecting, bool relayConnected)
    {
        return isConfigured
            ? isConnecting
                ? "Conectando"
                : relayConnected
                    ? "Relay conectado"
                    : "Relay configurado"
            : enabled
                ? "Configuracion incompleta"
                : "Desactivado";
    }

    public static string BuildAlexaSidebarStatusText(
        bool backgroundEnabled,
        string backgroundOnEventName,
        bool turnOffAfterEvent,
        string backgroundOffEventName)
    {
        var background = backgroundEnabled
            ? $"Fondo: {backgroundOnEventName}"
            : "Fondo sin mantener";

        var endBehavior = turnOffAfterEvent
            ? $"Al finalizar: {backgroundOffEventName}"
            : "Al finalizar: conserva estado";

        return $"{background}. {endBehavior}.";
    }

    private static string FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return "";
    }
}

public sealed record ChannelDisplayText(string Name, string Login);

public sealed record LightsArduinoStatusText(string Device, string Port, string LedCount, string Pins);
