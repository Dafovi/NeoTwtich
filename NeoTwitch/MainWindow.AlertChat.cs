using NeoTwitch.Models;
using NeoTwitch.Services;
using NeoTwitch.ViewModels.Activity;

namespace NeoTwitch;

public partial class MainWindow
{
    private async Task SendRuleChatMessageAsync(EventRule rule, TwitchEvent twitchEvent)
    {
        if (!rule.SendChatMessage)
        {
            return;
        }

        var message = _chatService.FormatMessage(rule.ChatMessageTemplate, twitchEvent);
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        try
        {
            await _authService.EnsureValidTokenAsync(_config, AddLog, CancellationToken.None);
            SaveConfig();
            await _chatService.SendMessageAsync(_config, message, CancellationToken.None);
            AddLog($"Chat enviado: {message}", ActivityLogKind.Twitch);
        }
        catch (Exception ex)
        {
            CrashReporter.Log(ex, $"No se pudo enviar mensaje de chat para la regla '{rule.Name}'.");
            AddLog($"Chat: {ex.Message}");
        }
    }
}
