using NeoTwitch.Models;
using NeoTwitch.Services.Streaming;

namespace NeoTwitch;

public partial class MainWindow
{
    private void UpdateRulePlatformAvailability()
    {
        var twitchEnabled = StreamingPlatformConfigurationService
            .GetOrCreate(_config.StreamingPlatforms, StreamingPlatform.Twitch)
            .IsEnabled;
        var youTubeEnabled = StreamingPlatformConfigurationService
            .GetOrCreate(_config.StreamingPlatforms, StreamingPlatform.YouTube)
            .IsEnabled;

        _alertsViewModel.Editor.UpdateSourceAvailability(twitchEnabled, youTubeEnabled);
    }
}
