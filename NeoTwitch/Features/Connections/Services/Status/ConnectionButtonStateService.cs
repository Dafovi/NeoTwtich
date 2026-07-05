namespace NeoTwitch.Services.Status;

public sealed record ConnectionButtonState(bool IsEnabled, string Content, string IconKey);

public sealed record ConnectionButtonLabels(
    string TwitchAuthorizing,
    string Connecting,
    string TwitchDisconnect,
    string TwitchConnect,
    string ArduinoConnect,
    string AlexaTesting,
    string AlexaTest,
    string ObsDisconnect,
    string ObsConnect,
    string ObsScenesUpdating,
    string ObsScenesRefresh);

public static class ConnectionButtonStateService
{
    public static ConnectionButtonState ResolveTwitch(
        bool isAuthorizing,
        bool isConnecting,
        bool isRunning,
        ConnectionButtonLabels labels)
    {
        var busy = isAuthorizing || isConnecting;
        var content = isAuthorizing
            ? labels.TwitchAuthorizing
            : isConnecting
                ? labels.Connecting
                : isRunning
                    ? labels.TwitchDisconnect
                    : labels.TwitchConnect;

        return new ConnectionButtonState(!busy, content, "Plug");
    }

    public static ConnectionButtonState ResolveArduino(bool enabled, bool isConnecting, ConnectionButtonLabels labels)
    {
        return new ConnectionButtonState(
            enabled && !isConnecting,
            isConnecting ? labels.Connecting : labels.ArduinoConnect,
            "Plug");
    }

    public static ConnectionButtonState ResolveAlexa(bool enabled, bool isConnecting, ConnectionButtonLabels labels)
    {
        return new ConnectionButtonState(
            enabled && !isConnecting,
            isConnecting ? labels.AlexaTesting : labels.AlexaTest,
            "Play");
    }

    public static ConnectionButtonState ResolveObs(
        bool enabled,
        bool isConnecting,
        bool isSceneActionRunning,
        bool isConnected,
        ConnectionButtonLabels labels)
    {
        var busy = isConnecting || isSceneActionRunning;
        var content = isConnecting
            ? labels.Connecting
            : isConnected
                ? labels.ObsDisconnect
                : labels.ObsConnect;

        return new ConnectionButtonState(enabled && !busy, content, "Plug");
    }

    public static ConnectionButtonState ResolveObsTest(
        bool enabled,
        bool isConnecting,
        bool isSceneActionRunning,
        ConnectionButtonLabels labels)
    {
        var busy = isConnecting || isSceneActionRunning;
        return new ConnectionButtonState(
            enabled && !busy,
            isConnecting ? labels.ObsScenesUpdating : labels.ObsScenesRefresh,
            "Refresh");
    }
}
