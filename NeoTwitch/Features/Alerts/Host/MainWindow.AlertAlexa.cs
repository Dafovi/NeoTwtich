using NeoTwitch.Models;
using NeoTwitch.Services;
using NeoTwitch.ViewModels.Activity;

namespace NeoTwitch;

public partial class MainWindow
{
    private async Task SendRuleAlexaEventAsync(
        EventRule rule,
        TwitchEvent twitchEvent,
        CancellationToken cancellationToken)
    {
        if (!rule.SendAlexaEvent || !_config.Alexa.IsConfigured)
        {
            return;
        }

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            await _alexaRelayService.SendRuleEventAsync(_config, rule, twitchEvent, cancellationToken);
            _alexaRelayConnected = true;
            AddLog($"Alexa: evento enviado para '{rule.Name}'.", ActivityLogKind.Alexa);
        }
        catch
        {
            _alexaRelayConnected = false;
            throw;
        }
        finally
        {
            UpdateAlexaStatusText();
        }
    }
}
