using NeoTwitch.Services.Status;
using NeoTwitch.ViewModels.Status;

namespace NeoTwitch.Services.Dashboard;

public sealed record DashboardConnectionStateInput(
    bool TwitchAuthorizing,
    bool TwitchConnecting,
    bool TwitchHasConnectionError,
    bool TwitchHasToken,
    bool YouTubeAuthorizing,
    bool YouTubeHasConnectionError,
    bool YouTubeHasToken,
    bool ArduinoEnabled,
    bool ArduinoConnecting,
    bool ArduinoHasConfirmedAck,
    bool ArduinoCompatibleWithoutAck,
    bool ArduinoHasOpenPort,
    bool AlexaEnabled,
    bool AlexaConnecting,
    bool AlexaIsConfigured,
    bool AlexaRelayConnected,
    bool ObsEnabled,
    bool ObsConnecting,
    bool ObsIsConnected,
    bool ObsHasConnectionError);

public sealed record DashboardConnectionStates(
    ConnectionVisualState Twitch,
    ConnectionVisualState YouTube,
    ConnectionVisualState Arduino,
    ConnectionVisualState Alexa,
    ConnectionVisualState Obs);

public static class DashboardConnectionStateService
{
    public static DashboardConnectionStates Resolve(DashboardConnectionStateInput input)
    {
        return new DashboardConnectionStates(
            ConnectionStateService.ResolveTwitch(
                input.TwitchAuthorizing,
                input.TwitchConnecting,
                input.TwitchHasConnectionError,
                input.TwitchHasToken),
            ConnectionStateService.ResolveYouTube(
                input.YouTubeAuthorizing,
                input.YouTubeHasConnectionError,
                input.YouTubeHasToken),
            ConnectionStateService.ResolveArduino(
                input.ArduinoEnabled,
                input.ArduinoConnecting,
                input.ArduinoHasConfirmedAck,
                input.ArduinoCompatibleWithoutAck,
                input.ArduinoHasOpenPort),
            ConnectionStateService.ResolveAlexa(
                input.AlexaEnabled,
                input.AlexaConnecting,
                input.AlexaIsConfigured,
                input.AlexaRelayConnected),
            ConnectionStateService.ResolveObs(
                input.ObsEnabled,
                input.ObsConnecting,
                input.ObsIsConnected,
                input.ObsHasConnectionError));
    }
}
