using NeoTwitch.Services.Text;

namespace NeoTwitch.Services.Dashboard;

public static class DashboardStatusTextLabelFactory
{
    public static DashboardStatusTextLabels Build(IUiTextService text)
    {
        return new DashboardStatusTextLabels(
            text.Get(UiTextKeys.DashboardNoTwitch),
            text.Get(UiTextKeys.DashboardNoLogin),
            text.Get(UiTextKeys.DashboardDefaultChannelName),
            text.Get(UiTextKeys.DashboardTwitchAuthorizing),
            text.Get(UiTextKeys.ConnectionConnecting),
            text.Get(UiTextKeys.ObsReviewConnection),
            text.Get(UiTextKeys.DashboardTwitchEventsConnected),
            text.Get(UiTextKeys.DashboardTwitchSessionAuthorized),
            text.Get(UiTextKeys.DashboardTwitchDisconnected),
            text.Get(UiTextKeys.DashboardTwitchWaitingAuthorization),
            text.Get(UiTextKeys.DashboardTwitchConnectingEvents),
            text.Get(UiTextKeys.DashboardTwitchLiveWithGameFormat),
            text.Get(UiTextKeys.DashboardTwitchLiveFormat),
            text.Get(UiTextKeys.DashboardTwitchOffline),
            text.Get(UiTextKeys.DashboardTwitchListeningUnqueried),
            text.Get(UiTextKeys.DashboardTwitchReady),
            text.Get(UiTextKeys.ConnectionDisabled),
            text.Get(UiTextKeys.ConnectionConnecting),
            text.Get(UiTextKeys.DashboardArduinoConnectedFormat),
            text.Get(UiTextKeys.DashboardArduinoDefaultPort),
            text.Get(UiTextKeys.DashboardArduinoVerifying),
            text.Get(UiTextKeys.DashboardArduinoDisconnected),
            text.Get(UiTextKeys.DashboardArduinoDisabledStatus),
            text.Get(UiTextKeys.DashboardArduinoConnectingStatusFormat),
            text.Get(UiTextKeys.DashboardArduinoConfiguredPortFallback),
            text.Get(UiTextKeys.DashboardArduinoBackgroundFormat),
            text.Get(UiTextKeys.DashboardArduinoBackgroundOff),
            text.Get(UiTextKeys.DashboardArduinoAckStatusFormat),
            text.Get(UiTextKeys.DashboardArduinoCompatibleStatusFormat),
            text.Get(UiTextKeys.DashboardArduinoOpenPortStatus),
            text.Get(UiTextKeys.DashboardArduinoPortSummaryFormat),
            text.Get(UiTextKeys.DashboardArduinoNoCom),
            text.Get(UiTextKeys.DashboardLightsNoPins),
            text.Get(UiTextKeys.DashboardLightsPinFormat),
            text.Get(UiTextKeys.DashboardLightsVerifying),
            text.Get(UiTextKeys.ConnectionConnected),
            text.Get(UiTextKeys.ConnectionDisconnected),
            text.Get(UiTextKeys.ConnectionConnecting),
            text.Get(UiTextKeys.DashboardAlexaReady),
            text.Get(UiTextKeys.DashboardAlexaMissingUrl),
            text.Get(UiTextKeys.DashboardAlexaDisabled),
            text.Get(UiTextKeys.DashboardAlexaRelayConnected),
            text.Get(UiTextKeys.DashboardAlexaRelayConfigured),
            text.Get(UiTextKeys.DashboardAlexaIncomplete),
            text.Get(UiTextKeys.DashboardAlexaBackgroundFormat),
            text.Get(UiTextKeys.DashboardAlexaBackgroundOff),
            text.Get(UiTextKeys.DashboardAlexaEndOffFormat),
            text.Get(UiTextKeys.DashboardAlexaEndKeep),
            text.Get(UiTextKeys.DashboardAlexaSidebarFormat));
    }
}
