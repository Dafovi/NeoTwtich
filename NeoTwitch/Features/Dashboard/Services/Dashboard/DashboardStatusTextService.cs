using NeoTwitch.Models;
using NeoTwitch.ViewModels.Status;

namespace NeoTwitch.Services.Dashboard;

public static class DashboardStatusTextService
{
    public static ChannelDisplayText BuildChannelDisplayText(
        bool channelReady,
        string? displayName,
        string? login,
        DashboardStatusTextLabels labels)
    {
        if (!channelReady)
        {
            return new ChannelDisplayText(labels.NoTwitch, labels.NoLogin);
        }

        var normalizedLogin = FirstNonEmpty(login, "");
        var channelName = FirstNonEmpty(displayName, normalizedLogin, labels.DefaultChannelName);
        var loginText = string.IsNullOrWhiteSpace(normalizedLogin)
            ? labels.NoLogin
            : $"@{normalizedLogin}";

        return new ChannelDisplayText(channelName, loginText);
    }

    public static string BuildTwitchConnectionText(
        bool isAuthorizing,
        bool isConnecting,
        bool hasConnectionError,
        bool eventSubRunning,
        bool hasToken,
        DashboardStatusTextLabels labels)
    {
        if (isAuthorizing)
        {
            return labels.TwitchAuthorizing;
        }

        if (isConnecting)
        {
            return labels.TwitchConnecting;
        }

        if (hasConnectionError)
        {
            return labels.TwitchReviewConnection;
        }

        if (eventSubRunning)
        {
            return labels.TwitchEventsConnected;
        }

        return hasToken ? labels.TwitchSessionAuthorized : labels.TwitchDisconnected;
    }

    public static string BuildTwitchStatusText(
        bool isAuthorizing,
        bool isConnecting,
        TwitchStreamStatus? streamStatus,
        bool eventSubRunning,
        DashboardStatusTextLabels labels)
    {
        if (isAuthorizing)
        {
            return labels.TwitchWaitingAuthorization;
        }

        if (isConnecting)
        {
            return labels.TwitchConnectingEvents;
        }

        if (streamStatus is { IsLive: true } live)
        {
            return string.IsNullOrWhiteSpace(live.GameName)
                ? string.Format(labels.TwitchLiveFormat, live.ViewerCount)
                : string.Format(labels.TwitchLiveWithGameFormat, live.GameName, live.ViewerCount);
        }

        if (streamStatus is { IsLive: false })
        {
            return labels.TwitchOffline;
        }

        return eventSubRunning
            ? labels.TwitchListeningUnqueried
            : labels.TwitchReady;
    }

    public static string BuildArduinoConnectionText(
        bool arduinoEnabled,
        bool isConnecting,
        bool hasConfirmedAck,
        bool compatibleWithoutAck,
        bool hasOpenPort,
        string? currentPort,
        DashboardStatusTextLabels labels)
    {
        if (!arduinoEnabled)
        {
            return labels.ArduinoDisabled;
        }

        if (isConnecting)
        {
            return labels.ArduinoConnecting;
        }

        if (hasConfirmedAck || compatibleWithoutAck)
        {
            return string.Format(labels.ArduinoConnectedFormat, FirstNonEmpty(currentPort, labels.ArduinoDefaultPort));
        }

        return hasOpenPort ? labels.ArduinoVerifying : labels.ArduinoDisconnected;
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
        string backgroundPatternName,
        DashboardStatusTextLabels labels)
    {
        if (!arduinoEnabled)
        {
            return labels.ArduinoDisabledStatus;
        }

        if (isConnecting)
        {
            return string.Format(labels.ArduinoConnectingStatusFormat, FirstNonEmpty(serialPort, labels.ArduinoConfiguredPortFallback));
        }

        if (hasConfirmedAck)
        {
            var activeBackground = backgroundEnabled
                ? string.Format(labels.ArduinoBackgroundFormat, backgroundPatternName)
                : labels.ArduinoBackgroundOff;
            return string.Format(labels.ArduinoAckStatusFormat, baudRate, stripCount, totalLeds, activeBackground);
        }

        if (compatibleWithoutAck)
        {
            return string.Format(labels.ArduinoCompatibleStatusFormat, baudRate);
        }

        if (hasOpenPort)
        {
            return labels.ArduinoOpenPortStatus;
        }

        return string.Format(labels.ArduinoPortSummaryFormat, FirstNonEmpty(serialPort, labels.ArduinoNoCom), stripCount, totalLeds);
    }

    public static LightsArduinoStatusText BuildLightsArduinoStatusText(
        bool arduinoEnabled,
        bool hasConfirmedAck,
        bool compatibleWithoutAck,
        bool hasOpenPort,
        string? currentPort,
        string? configuredPort,
        IEnumerable<LedStripConfig> ledStrips,
        DashboardStatusTextLabels labels)
    {
        var strips = ledStrips.ToList();
        var totalLeds = strips.Sum(strip => strip.LedCount);
        var pins = strips.Count == 0
            ? labels.LightsNoPins
            : string.Join(", ", strips.Select(strip => string.Format(labels.LightsPinFormat, strip.Pin)));
        var device = !arduinoEnabled
            ? labels.ArduinoDisabled
            : hasConfirmedAck || compatibleWithoutAck
                ? labels.ConnectionConnected
                : hasOpenPort
                    ? labels.LightsVerifying
                    : labels.ConnectionDisconnected;
        var port = hasOpenPort
            ? FirstNonEmpty(currentPort, configuredPort, labels.ArduinoNoCom)
            : FirstNonEmpty(configuredPort, labels.ArduinoNoCom);

        return new LightsArduinoStatusText(device, port, totalLeds.ToString(), pins);
    }

    public static string BuildAlexaStatusText(bool enabled, bool isConfigured, DashboardStatusTextLabels labels)
    {
        return isConfigured
            ? labels.AlexaReady
            : enabled
                ? labels.AlexaMissingUrl
                : labels.AlexaDisabled;
    }

    public static string BuildAlexaConnectionText(
        bool enabled,
        bool isConfigured,
        bool isConnecting,
        bool relayConnected,
        DashboardStatusTextLabels labels)
    {
        return isConfigured
            ? isConnecting
                ? labels.ConnectionConnecting
                : relayConnected
                    ? labels.AlexaRelayConnected
                    : labels.AlexaRelayConfigured
            : enabled
                ? labels.AlexaIncomplete
                : labels.ArduinoDisabled;
    }

    public static string BuildAlexaSidebarStatusText(
        bool backgroundEnabled,
        string backgroundOnEventName,
        bool turnOffAfterEvent,
        string backgroundOffEventName,
        DashboardStatusTextLabels labels)
    {
        var background = backgroundEnabled
            ? string.Format(labels.AlexaBackgroundFormat, backgroundOnEventName)
            : labels.AlexaBackgroundOff;

        var endBehavior = turnOffAfterEvent
            ? string.Format(labels.AlexaEndOffFormat, backgroundOffEventName)
            : labels.AlexaEndKeep;

        return string.Format(labels.AlexaSidebarFormat, background, endBehavior);
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

public sealed record DashboardStatusTextLabels(
    string NoTwitch,
    string NoLogin,
    string DefaultChannelName,
    string TwitchAuthorizing,
    string TwitchConnecting,
    string TwitchReviewConnection,
    string TwitchEventsConnected,
    string TwitchSessionAuthorized,
    string TwitchDisconnected,
    string TwitchWaitingAuthorization,
    string TwitchConnectingEvents,
    string TwitchLiveWithGameFormat,
    string TwitchLiveFormat,
    string TwitchOffline,
    string TwitchListeningUnqueried,
    string TwitchReady,
    string ArduinoDisabled,
    string ArduinoConnecting,
    string ArduinoConnectedFormat,
    string ArduinoDefaultPort,
    string ArduinoVerifying,
    string ArduinoDisconnected,
    string ArduinoDisabledStatus,
    string ArduinoConnectingStatusFormat,
    string ArduinoConfiguredPortFallback,
    string ArduinoBackgroundFormat,
    string ArduinoBackgroundOff,
    string ArduinoAckStatusFormat,
    string ArduinoCompatibleStatusFormat,
    string ArduinoOpenPortStatus,
    string ArduinoPortSummaryFormat,
    string ArduinoNoCom,
    string LightsNoPins,
    string LightsPinFormat,
    string LightsVerifying,
    string ConnectionConnected,
    string ConnectionDisconnected,
    string ConnectionConnecting,
    string AlexaReady,
    string AlexaMissingUrl,
    string AlexaDisabled,
    string AlexaRelayConnected,
    string AlexaRelayConfigured,
    string AlexaIncomplete,
    string AlexaBackgroundFormat,
    string AlexaBackgroundOff,
    string AlexaEndOffFormat,
    string AlexaEndKeep,
    string AlexaSidebarFormat);
