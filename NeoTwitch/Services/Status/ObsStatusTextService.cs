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

public readonly record struct ObsStatusTextLabels(
    string Disabled,
    string Connecting,
    string Connected,
    string Disconnected,
    string ReviewConnection,
    string DisabledStatusText,
    string ConnectedStatusTextFormat,
    string ConnectPromptStatusText,
    string NoScene,
    string DefaultHost,
    string NoVersion,
    string StudioModeEnabled,
    string StudioModeDisabled);

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
        bool studioMode,
        ObsStatusTextLabels labels)
    {
        var hasError = !string.IsNullOrWhiteSpace(connectionError);
        var state = !enabled
            ? labels.Disabled
            : isConnecting
                ? labels.Connecting
                : isConnected
                    ? labels.Connected
                    : hasError
                        ? labels.ReviewConnection
                        : labels.Disconnected;

        var statusText = !enabled
            ? labels.DisabledStatusText
            : isConnected
                ? string.Format(labels.ConnectedStatusTextFormat, FirstNonEmpty(host, labels.DefaultHost), port)
                : hasError
                    ? connectionError!.Trim()
                    : labels.ConnectPromptStatusText;

        return new ObsStatusText(
            state,
            statusText,
            FirstNonEmpty(currentScene, labels.NoScene),
            FirstNonEmpty(host, labels.DefaultHost),
            port.ToString(),
            FirstNonEmpty(version, labels.NoVersion),
            Math.Max(0, sceneCount).ToString(),
            studioMode ? labels.StudioModeEnabled : labels.StudioModeDisabled);
    }

    private static string FirstNonEmpty(string? value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }
}
