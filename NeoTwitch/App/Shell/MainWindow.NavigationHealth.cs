using NeoTwitch.Models;
using NeoTwitch.Services.Dashboard;
using NeoTwitch.Services.Status;
using NeoTwitch.Services.Text;
using NeoTwitch.Services.Ui;
using NeoTwitch.ViewModels.Status;
using WpfButton = System.Windows.Controls.Button;

namespace NeoTwitch;

public partial class MainWindow
{
    private void UpdateNavigationHealthIndicators(
        DashboardConnectionStates states,
        ConnectionStateVisual twitchVisual,
        ConnectionStateVisual arduinoVisual,
        ConnectionStateVisual alexaVisual,
        ConnectionStateVisual obsVisual)
    {
        var twitchIssue = BuildNavigationIssue("Twitch", states.Twitch, twitchVisual, "Twitch necesita estar conectado para recibir eventos.");
        var arduinoIssue = _config.ArduinoEnabled
            ? BuildNavigationIssue(_text.Get(UiTextKeys.NavLights), states.Arduino, arduinoVisual, "Las luces no van a funcionar porque Arduino no esta conectado o no respondio.")
            : null;
        var alexaIssue = _config.Alexa.Enabled
            ? BuildNavigationIssue(_text.Get(UiTextKeys.NavAlexa), states.Alexa, alexaVisual, "Alexa no va a funcionar porque falta configurar o conectar el relay.")
            : null;
        var obsIssue = _config.Obs.Enabled
            ? BuildNavigationIssue(_text.Get(UiTextKeys.NavObs), states.Obs, obsVisual, "OBS no va a funcionar porque esta activo pero no esta conectado.")
            : null;
        var mediaIssue = _config.Obs.Enabled && IsNavigationIssue(states.Obs)
            ? BuildNavigationIssue(_text.Get(UiTextKeys.NavObs), states.Obs, obsVisual, "Imagenes y videos necesitan OBS conectado para mostrarse en escena.")
            : null;

        ApplyNavigationIssue(NavConnectionsButton, _text.Get(UiTextKeys.NavConnections), PickWorstIssue(twitchIssue, arduinoIssue, alexaIssue, obsIssue));
        ApplyNavigationIssue(NavRulesButton, _text.Get(UiTextKeys.NavAlerts), BuildAlertsNavigationIssue(states, arduinoVisual, alexaVisual, obsVisual));
        ApplyNavigationIssue(NavObsButton, _text.Get(UiTextKeys.NavObs), obsIssue);
        ApplyNavigationIssue(NavStripsButton, _text.Get(UiTextKeys.NavLights), arduinoIssue);
        ApplyNavigationIssue(NavAlexaButton, _text.Get(UiTextKeys.NavAlexa), alexaIssue);
        ApplyNavigationIssue(NavImagesButton, _text.Get(UiTextKeys.NavImages), mediaIssue);
        ApplyNavigationIssue(NavVideosButton, _text.Get(UiTextKeys.NavVideos), mediaIssue);

        ApplyNavigationIssue(NavSettingsButton, _text.Get(UiTextKeys.NavPanel), null);
        ApplyNavigationIssue(NavAudioButton, _text.Get(UiTextKeys.NavAudio), null);
        ApplyNavigationIssue(NavActivityButton, _text.Get(UiTextKeys.NavActivity), null);
        ApplyNavigationIssue(NavPreferencesButton, _text.Get(UiTextKeys.NavConfiguration), null);
    }

    private NavigationIssue? BuildAlertsNavigationIssue(
        DashboardConnectionStates states,
        ConnectionStateVisual arduinoVisual,
        ConnectionStateVisual alexaVisual,
        ConnectionStateVisual obsVisual)
    {
        var activeRules = _config.Rules.Where(rule => rule.IsEnabled).ToArray();
        if (activeRules.Length == 0)
        {
            return null;
        }

        List<NavigationIssue?> issues = [];
        if (activeRules.Any(rule => rule.UseLights) && IsNavigationIssue(states.Arduino))
        {
            issues.Add(BuildNavigationIssue(_text.Get(UiTextKeys.NavLights), states.Arduino, arduinoVisual, "Hay alertas activas con luces, pero Arduino no esta listo."));
        }

        if (activeRules.Any(UsesObsAction) && IsNavigationIssue(states.Obs))
        {
            issues.Add(BuildNavigationIssue(_text.Get(UiTextKeys.NavObs), states.Obs, obsVisual, "Hay alertas activas con acciones de OBS, pero OBS no esta listo."));
        }

        if (activeRules.Any(rule => rule.SendAlexaEvent) && IsNavigationIssue(states.Alexa))
        {
            issues.Add(BuildNavigationIssue(_text.Get(UiTextKeys.NavAlexa), states.Alexa, alexaVisual, "Hay alertas activas con Alexa, pero Alexa no esta lista."));
        }

        return PickWorstIssue(issues.ToArray());
    }

    private static bool UsesObsAction(EventRule rule)
    {
        return rule.SendObsScene
            || rule.SendObsMedia
            || rule.SendObsImage
            || rule.SendObsVideo
            || (rule.UseVirtualLights && rule.VirtualLightsToObs);
    }

    private static NavigationIssue? BuildNavigationIssue(
        string label,
        ConnectionVisualState state,
        ConnectionStateVisual visual,
        string tooltip)
    {
        if (!IsNavigationIssue(state))
        {
            return null;
        }

        return new NavigationIssue(
            state,
            visual.IconPath,
            visual.Color,
            $"{label}: {visual.Text}. {tooltip}");
    }

    private static NavigationIssue? PickWorstIssue(params NavigationIssue?[] issues)
    {
        return issues
            .Where(issue => issue is not null)
            .OrderByDescending(issue => NavigationIssueSeverity(issue!.Value.State))
            .FirstOrDefault();
    }

    private static bool IsNavigationIssue(ConnectionVisualState state)
    {
        return state is ConnectionVisualState.Disconnected
            or ConnectionVisualState.Warning
            or ConnectionVisualState.Connecting;
    }

    private static int NavigationIssueSeverity(ConnectionVisualState state)
    {
        return state switch
        {
            ConnectionVisualState.Disconnected => 30,
            ConnectionVisualState.Warning => 20,
            ConnectionVisualState.Connecting => 10,
            _ => 0
        };
    }

    private static void ApplyNavigationIssue(WpfButton button, string defaultTooltip, NavigationIssue? issue)
    {
        ButtonIconContentService.SetNavigationStatus(
            button,
            issue?.IconPath ?? "Assets/Icons/status_empty.png",
            issue?.Tooltip ?? "",
            issue is not null,
            defaultTooltip,
            issue?.Color);
    }

    private readonly record struct NavigationIssue(
        ConnectionVisualState State,
        string IconPath,
        string Color,
        string Tooltip);
}
