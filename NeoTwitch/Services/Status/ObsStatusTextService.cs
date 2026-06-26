namespace NeoTwitch.Services.Status;

public readonly record struct ObsStatusText(
    string State,
    string StatusText,
    string CurrentScene,
    string Host,
    string Port,
    string Version,
    string SceneCount,
    string StudioMode);

public static class ObsStatusTextService
{
    public static ObsStatusText Build(
        bool enabled,
        bool isConnecting,
        bool isConnected,
        string? connectionError,
        string? currentScene,
        string? host,
        int port,
        string? version,
        int sceneCount,
        bool studioMode)
    {
        var hasError = !string.IsNullOrWhiteSpace(connectionError);
        var state = !enabled
            ? "Desactivado"
            : isConnecting
                ? "Conectando"
                : isConnected
                    ? "Conectado"
                    : hasError
                        ? "Revisar conexion"
                        : "Desconectado";

        var statusText = !enabled
            ? "OBS desactivado. Las acciones OBS no se mostraran ni ejecutaran."
            : isConnected
                ? $"OBS conectado en {FirstNonEmpty(host, "127.0.0.1")}:{port}."
                : hasError
                    ? connectionError!.Trim()
                    : "Conecta OBS Studio para leer escenas y preparar automatizaciones.";

        return new ObsStatusText(
            state,
            statusText,
            FirstNonEmpty(currentScene, "Sin escena"),
            FirstNonEmpty(host, "127.0.0.1"),
            port.ToString(),
            FirstNonEmpty(version, "Sin version"),
            Math.Max(0, sceneCount).ToString(),
            studioMode ? "Activado" : "Desactivado");
    }

    private static string FirstNonEmpty(string? value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }
}
