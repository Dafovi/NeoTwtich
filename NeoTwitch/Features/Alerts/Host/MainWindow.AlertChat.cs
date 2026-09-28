using NeoTwitch.Models;
using NeoTwitch.Services;
using NeoTwitch.Services.Alerts;
using NeoTwitch.ViewModels.Activity;

namespace NeoTwitch;

public partial class MainWindow
{
    private async Task SendRuleChatMessageAsync(
        AlertExecutionRequest request,
        CancellationToken cancellationToken)
    {
        var rule = request.Rule;
        if (!rule.Chat.Enabled)
        {
            return;
        }

        var message = _chatService.FormatMessage(rule.Chat.MessageTemplate, request.Trigger.ToTwitchEvent());
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        cancellationToken.ThrowIfCancellationRequested();
        if (request.TriggerPlatform == StreamingPlatform.YouTube)
        {
            await SendYouTubeRuleChatMessageAsync(message, cancellationToken);
            return;
        }

        await _authService.EnsureValidTokenAsync(_config, AddLog, cancellationToken);
        SaveConfig();
        await _chatService.SendMessageAsync(_config, message, cancellationToken);
        AddLog($"Chat enviado: {message}", ActivityLogKind.Twitch);
    }

    private async Task SendYouTubeRuleChatMessageAsync(string message, CancellationToken cancellationToken)
    {
        await _youTubeAuthService.EnsureValidTokenAsync(_config, cancellationToken);
        SaveConfig();
        var broadcast = await _youTubeLiveService.GetActiveBroadcastAsync(_config.YouTubeToken, cancellationToken);
        if (!broadcast.IsLive || string.IsNullOrWhiteSpace(broadcast.LiveChatId))
        {
            throw new InvalidOperationException("No hay un directo de YouTube activo donde enviar el mensaje.");
        }

        await _youTubeLiveChatService.SendMessageAsync(
            _config.YouTubeToken,
            broadcast.LiveChatId,
            message,
            cancellationToken);
        AddLog($"Chat de YouTube enviado: {message}", ActivityLogKind.YouTube);
    }
}
