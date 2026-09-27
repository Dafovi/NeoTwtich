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
            _connectionsViewModel.UpdateYouTubeChannel(_config.YouTubeChannel);
            StreamingPlatformConfigurationService.GetOrCreate(_config.StreamingPlatforms, StreamingPlatform.YouTube).IsEnabled = true;
            SaveConfig();
            _youTubeConnectionError = "";

            try
            {
                _config.YouTubeChannel = await _youTubeChannelService.GetCurrentChannelAsync(_config.YouTubeToken, CancellationToken.None);
                _connectionsViewModel.UpdateYouTubeChannel(_config.YouTubeChannel);
                SaveConfig();
            }
            catch (Exception ex)
            {
                AddLog($"YouTube: cuenta autorizada, pero no se pudo leer el perfil todavía: {ex.Message}", ActivityLogKind.YouTube);
            }

            await _youTubePlatformProvider.StartAsync(CancellationToken.None);

            YouTubeLiveBroadcastStatus status;
            try
            {
                status = await _youTubeLiveService.GetActiveBroadcastAsync(_config.YouTubeToken, CancellationToken.None);
            }
            catch (Exception ex)
            {
                status = YouTubeLiveBroadcastStatus.Offline;
                AddLog($"YouTube: cuenta autorizada, pero no se pudo consultar el directo todavía: {ex.Message}", ActivityLogKind.YouTube);
            }

            AddLog(status.IsLive
                ? $"YouTube: cuenta autorizada para {_config.YouTubeChannel.DisplayName}. Directo activo: {status.Title}."
                : $"YouTube: cuenta autorizada para {_config.YouTubeChannel.DisplayName}. No hay un directo activo ahora.", ActivityLogKind.YouTube);
        }
        catch (Exception ex)
        {
            _youTubeConnectionError = ex.Message;
            AddLog($"YouTube: {ex.Message}", ActivityLogKind.YouTube);
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
        RefreshDashboardConnectionStates();
    }
}
