using NeoTwitch.ViewModels.Status;

namespace NeoTwitch.Services.Status;

public readonly record struct ConnectionStateVisual(string Text, string Color, string IconPath);

public static class ConnectionStateService
{
    public static ConnectionVisualState ResolveTwitch(
        bool isAuthorizing,
        bool isConnecting,
        bool hasConnectionError,
        bool hasToken)
    {
        if (isAuthorizing || isConnecting)
        {
            return ConnectionVisualState.Connecting;
        }

        if (hasConnectionError)
        {
            return ConnectionVisualState.Warning;
        }

        return hasToken
            ? ConnectionVisualState.Connected
            : ConnectionVisualState.Disconnected;
    }

    public static ConnectionVisualState ResolveArduino(
        bool enabled,
        bool isConnecting,
        bool hasConfirmedAck,
        bool isCompatibleWithoutAck,
        bool hasOpenPort)
    {
        if (!enabled)
        {
            return ConnectionVisualState.Disabled;
        }

        if (isConnecting)
        {
            return ConnectionVisualState.Connecting;
        }

        if (hasConfirmedAck || isCompatibleWithoutAck)
        {
            return ConnectionVisualState.Connected;
        }

        return hasOpenPort
            ? ConnectionVisualState.Connecting
            : ConnectionVisualState.Disconnected;
    }

    public static ConnectionVisualState ResolveAlexa(
        bool enabled,
        bool isConnecting,
        bool isConfigured,
        bool relayConnected)
    {
        if (!enabled)
        {
            return ConnectionVisualState.Disabled;
        }

        if (isConnecting)
        {
            return ConnectionVisualState.Connecting;
        }

        if (!isConfigured)
        {
            return ConnectionVisualState.Warning;
        }

        return relayConnected
            ? ConnectionVisualState.Connected
            : ConnectionVisualState.Warning;
    }

    public static ConnectionVisualState ResolveObs(
        bool enabled,
        bool isConnecting,
        bool isConnected,
        bool hasConnectionError)
    {
        if (!enabled)
        {
            return ConnectionVisualState.Disabled;
        }

        if (isConnecting)
        {
            return ConnectionVisualState.Connecting;
        }

        if (isConnected)
        {
            return ConnectionVisualState.Connected;
        }

        return hasConnectionError
            ? ConnectionVisualState.Warning
            : ConnectionVisualState.Disconnected;
    }

    public static ConnectionStateVisual GetVisual(
        ConnectionVisualState state,
        string connectedText = "Conectado",
        string disconnectedText = "Desconectado",
        string disabledText = "Desactivado",
        string connectingText = "Conectando",
        string warningText = "Revisar")
    {
        return state switch
        {
            ConnectionVisualState.Connected => new ConnectionStateVisual(connectedText, "#22C55E", "Assets/Icons/status_ok.png"),
            ConnectionVisualState.Connecting => new ConnectionStateVisual(connectingText, "#FFB020", "Assets/Icons/status_warning.png"),
            ConnectionVisualState.Warning => new ConnectionStateVisual(warningText, "#FFB020", "Assets/Icons/status_warning.png"),
            ConnectionVisualState.Disabled => new ConnectionStateVisual(disabledText, "#94A3B8", "Assets/Icons/status_empty.png"),
            _ => new ConnectionStateVisual(disconnectedText, "#F43F5E", "Assets/Icons/status_error.png")
        };
    }
}
