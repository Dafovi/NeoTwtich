using NeoTwitch.Models;
using NeoTwitch.Services;
using NeoTwitch.ViewModels.Activity;

namespace NeoTwitch;

public partial class MainWindow
{
    private async Task SendRuleAlexaEventAsync(EventRule rule, TwitchEvent twitchEvent)
    {
        if (!rule.SendAlexaEvent || !_config.Alexa.IsConfigured)
        {
            return;
        }

        try
        {
            await _alexaRelayService.SendRuleEventAsync(_config, rule, twitchEvent, CancellationToken.None);
            _alexaRelayConnected = true;
            AddLog($"Alexa: evento enviado para '{rule.Name}'.", ActivityLogKind.Alexa);
        }
        catch (Exception ex)
        {
            _alexaRelayConnected = false;
            CrashReporter.Log(ex, $"No se pudo enviar evento Alexa para la regla '{rule.Name}'.");
            AddLog($"Alexa: {ex.Message}", ActivityLogKind.Important);
        }
        finally
        {
            UpdateAlexaStatusText();
        }
    }
}
