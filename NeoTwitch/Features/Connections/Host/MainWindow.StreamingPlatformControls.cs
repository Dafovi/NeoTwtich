using System.Windows;

namespace NeoTwitch;

public partial class MainWindow
{
    internal async void StreamingPlatformEnabledChanged(object sender, RoutedEventArgs e)
    {
        if (_loadingUi || _initializingComponent)
        {
            return;
        }

        try
        {
            SaveGlobalSettingsFromFields();

            if (!_connectionsViewModel.TwitchEnabled && _twitchPlatformProvider.IsRunning)
            {
                await _twitchPlatformProvider.StopAsync();
                _eventSubscriptionSignature = "";
                _streamStatus = null;
                _twitchConnectionError = "";
            }

            if (!_connectionsViewModel.YouTubeEnabled && _youTubePlatformProvider.IsRunning)
            {
                await _youTubePlatformProvider.StopAsync();
                _youTubeConnectionError = "";
            }

            SaveConfig();
            UpdateYouTubeConnectionUi();
            UpdateStatusText();
        }
        catch (Exception ex)
        {
            AddLog($"Plataformas: no se pudo actualizar la conexión: {ex.Message}");
            UpdateStatusText();
        }
    }
}
