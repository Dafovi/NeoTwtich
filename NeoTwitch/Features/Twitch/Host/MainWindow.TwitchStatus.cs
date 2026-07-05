namespace NeoTwitch;

public partial class MainWindow
{
    private async Task RefreshTwitchStreamStatusAsync()
    {
        if (!_config.Token.HasToken || !_config.Channel.IsReady)
        {
            _streamStatus = null;
            UpdateStatusText();
            return;
        }

        try
        {
            await _authService.EnsureValidTokenAsync(_config, AddLog, CancellationToken.None);
            _streamStatus = await _authService.GetStreamStatusAsync(_config, CancellationToken.None);
            _twitchConnectionError = "";
            SaveConfig();
        }
        catch (Exception ex)
        {
            _streamStatus = null;
            _twitchConnectionError = ex.Message;
            AddLog($"Twitch estado: {ex.Message}");
        }
        finally
        {
            _lastStreamStatusRefreshAt = DateTimeOffset.UtcNow;
        }

        UpdateStatusText();
    }
}
