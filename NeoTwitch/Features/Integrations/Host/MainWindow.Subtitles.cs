using NeoTwitch.Models;

namespace NeoTwitch;

public partial class MainWindow
{
    private static string SubtitleSource(bool captions) => captions ? "NeoTwitch · Live Captions" : "NeoTwitch · Chat Translator";
    internal SubtitleIntegrationConfig GetSubtitleConfig(bool captions) => captions
        ? _config.LiveCaptions ??= new() : _config.ChatTranslator ??= new();
    internal void OpenSayonariProject(bool captions) => _externalLauncher.Open(captions
        ? "https://github.com/sayonari/jimakuChan" : "https://github.com/sayonari/twitchTransFreeNext");
    internal void ValidateSubtitleConnections(bool captions)
    {
        if (!_obsService.IsConnected) throw new InvalidOperationException("Conecta OBS desde Conexiones antes de activar.");
        if (!captions && !_eventSubClient.IsRunning) throw new InvalidOperationException("Conecta Twitch desde Conexiones antes de activar Chat Translator.");
    }
    internal async Task ShowSubtitleSourceAsync(bool captions, SubtitleIntegrationConfig config, string url, CancellationToken token)
    {
        var canvas = await _obsService.GetCanvasSizeAsync(token);
        await _obsService.ShowBrowserSourceAsync(config.Scene, SubtitleSource(captions), url, canvas.Width, canvas.Height, token);
    }
    internal async Task HideSubtitleSourceAsync(bool captions, string scene)
    {
        if (_obsService.IsConnected) await _obsService.HideSceneSourceAsync(scene, SubtitleSource(captions), CancellationToken.None);
    }
    internal void SaveSubtitleConfig(bool captions, SubtitleIntegrationConfig config)
    {
        if (captions) _config.LiveCaptions = config;
        else { _config.ChatTranslator = config; config.Running = true; ScheduleTwitchSubscriptionRefreshIfNeeded(); }
        SaveConfig();
    }
    internal void DisableSubtitleIntegration(bool captions)
    {
        if (captions) return;
        GetSubtitleConfig(false).Running = false;
        if (!_isExiting) ScheduleTwitchSubscriptionRefreshIfNeeded();
    }
    internal async Task PublishTranslationAsync(string user, string translation)
    {
        await Dispatcher.InvokeAsync(async () =>
        {
            var config = GetSubtitleConfig(false);
            if (!config.Running || !config.PublishToChat || _isExiting) return;
            var message = $"[NT Traducción] {user}: {translation}";
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(8));
            await _chatService.SendMessageAsync(_config, message[..Math.Min(message.Length, 500)], timeout.Token);
        }).Task.Unwrap();
    }
}
