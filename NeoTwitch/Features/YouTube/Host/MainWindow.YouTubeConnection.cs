using NeoTwitch.Models;
using NeoTwitch.Services.Status;
using NeoTwitch.Services.Streaming;
using NeoTwitch.Services.YouTube;
using NeoTwitch.ViewModels.Activity;
using NeoTwitch.ViewModels.Status;

namespace NeoTwitch;

public partial class MainWindow
{
    private bool _isYouTubeAuthorizing;
    private string _youTubeConnectionError = "";

    private async void ToggleYouTubeConnection()
    {
        if (_isYouTubeAuthorizing)
        {
            return;
        }

        SaveGlobalSettingsFromFields();
        if (string.IsNullOrWhiteSpace(_config.YouTubeClientSecret))
        {
            _dialog.ShowWarning("YouTube", "Pega el secreto del cliente de escritorio de YouTube antes de conectar.");
            return;
        }

        _isYouTubeAuthorizing = true;
        UpdateYouTubeConnectionUi();
        try
        {
            await using var callback = YouTubeLoopbackCallbackListener.Start();
            var request = _youTubeAuthService.BeginAuthorization(_config.YouTubeClientId, callback.RedirectUri);
            _youTubeAuthService.OpenAuthorizationPage(request);
            var code = await callback.WaitForAuthorizationCodeAsync(request.State, CancellationToken.None);
            _config.YouTubeToken = await _youTubeAuthService.ExchangeAuthorizationCodeAsync(
                _config.YouTubeClientId,
                _config.YouTubeClientSecret,
                request,
                code,
                CancellationToken.None);
            _config.YouTubeChannel = new YouTubeChannelInfo();
            StreamingPlatformConfigurationService.GetOrCreate(_config.StreamingPlatforms, StreamingPlatform.YouTube).IsEnabled = true;
            SaveConfig();
            _youTubeConnectionError = "";

            YouTubeLiveBroadcastStatus status;
            try
            {
                status = await _youTubeLiveService.GetActiveBroadcastAsync(_config.YouTubeToken, CancellationToken.None);
            }
            catch (Exception ex)
            {
                status = YouTubeLiveBroadcastStatus.Offline;
                AddLog($"YouTube: cuenta autorizada, pero no se pudo consultar el directo todavía: {ex.Message}", ActivityLogKind.Important);
            }

            AddLog(status.IsLive
                ? $"YouTube: cuenta autorizada. Directo activo: {status.Title}."
                : "YouTube: cuenta autorizada. No hay un directo activo ahora.", ActivityLogKind.Important);
        }
        catch (Exception ex)
        {
            _youTubeConnectionError = ex.Message;
            AddLog($"YouTube: {ex.Message}", ActivityLogKind.Important);
            _dialog.ShowWarning("YouTube", ex.Message);
        }
        finally
        {
            _isYouTubeAuthorizing = false;
            UpdateYouTubeConnectionUi();
        }
    }

    private void UpdateYouTubeConnectionUi()
    {
        var state = _isYouTubeAuthorizing
            ? ConnectionVisualState.Connecting
            : !string.IsNullOrWhiteSpace(_youTubeConnectionError)
                ? ConnectionVisualState.Warning
                : _config.YouTubeToken.HasToken
                    ? ConnectionVisualState.Connected
                    : ConnectionVisualState.Disconnected;
        var labels = new ConnectionStateLabels("Conectado", "Desconectado", "Desactivado", "Autorizando", "Revisar");
        var button = new ConnectionButtonState(
            !_isYouTubeAuthorizing,
            _isYouTubeAuthorizing ? "Autorizando..." : _config.YouTubeToken.HasToken ? "Reconectar YouTube" : "Conectar YouTube",
            "Plug");
        _connectionsViewModel.UpdateYouTubeConnection(ConnectionStateService.GetVisual(state, labels), button);
    }
}
