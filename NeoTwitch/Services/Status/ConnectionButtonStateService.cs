namespace NeoTwitch.Services.Status;

public sealed record ConnectionButtonState(bool IsEnabled, string Content);

public static class ConnectionButtonStateService
{
    public static ConnectionButtonState ResolveTwitch(bool isAuthorizing, bool isConnecting, bool isRunning)
    {
        var busy = isAuthorizing || isConnecting;
        var content = isAuthorizing
            ? "Autorizando..."
            : isConnecting
                ? "Conectando..."
                : isRunning
                    ? "Desconectar Twitch"
                    : "Conectar Twitch";

        return new ConnectionButtonState(!busy, content);
    }

    public static ConnectionButtonState ResolveArduino(bool enabled, bool isConnecting)
    {
        return new ConnectionButtonState(
            enabled && !isConnecting,
            isConnecting ? "Conectando..." : "Conectar Arduino");
    }

    public static ConnectionButtonState ResolveAlexa(bool enabled, bool isConnecting)
    {
        return new ConnectionButtonState(
            enabled && !isConnecting,
            isConnecting ? "Probando..." : "Probar Alexa");
    }

    public static ConnectionButtonState ResolveObs(bool enabled, bool isConnecting, bool isSceneActionRunning, bool isConnected)
    {
        var busy = isConnecting || isSceneActionRunning;
        var content = isConnecting
            ? "Conectando..."
            : isConnected
                ? "Desconectar OBS"
                : "Conectar OBS";

        return new ConnectionButtonState(enabled && !busy, content);
    }

    public static ConnectionButtonState ResolveObsTest(bool enabled, bool isConnecting, bool isSceneActionRunning)
    {
        var busy = isConnecting || isSceneActionRunning;
        return new ConnectionButtonState(
            enabled && !busy,
            isConnecting ? "Actualizando..." : "Actualizar escenas");
    }
}
