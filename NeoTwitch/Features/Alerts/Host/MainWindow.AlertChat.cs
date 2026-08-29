using NeoTwitch.Models;
using NeoTwitch.Services;
using NeoTwitch.ViewModels.Activity;

namespace NeoTwitch;

public partial class MainWindow
{
    private async Task SendRuleChatMessageAsync(
        AlertExecutionRuleSnapshot rule,
        AlertTriggerSnapshot trigger,
        CancellationToken cancellationToken)
    {
        if (!rule.Chat.Enabled)
        {
            return;
        }

        var message = _chatService.FormatMessage(rule.Chat.MessageTemplate, trigger.ToTwitchEvent());
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        cancellationToken.ThrowIfCancellationRequested();
        await _authService.EnsureValidTokenAsync(_config, AddLog, cancellationToken);
        SaveConfig();
        await _chatService.SendMessageAsync(_config, message, cancellationToken);
        AddLog($"Chat enviado: {message}", ActivityLogKind.Twitch);
    }
}
