using NeoTwitch.ViewModels.Status;

namespace NeoTwitch.Services.Dashboard;

public static class DashboardStatusTextService
{
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
}
