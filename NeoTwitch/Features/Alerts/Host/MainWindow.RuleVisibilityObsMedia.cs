using NeoTwitch.Models;
using NeoTwitch.Services.Alerts;
using NeoTwitch.ViewModels.Library;

namespace NeoTwitch;

public partial class MainWindow
{
    private void RefreshRuleObsMediaChoices()
    {
        if (_initializingComponent)
        {
            return;
        }

        var imageChoices = RuleObsMediaChoiceService.Resolve(
            ObsMediaKind.Image,
            _config.ImageLibrary,
            _config.VideoLibrary,
            _config.ImageGroups,
            _config.VideoGroups);
        var videoChoices = RuleObsMediaChoiceService.Resolve(
            ObsMediaKind.Video,
            _config.ImageLibrary,
            _config.VideoLibrary,
            _config.ImageGroups,
            _config.VideoGroups);

        _alertsViewModel.UpdateObsMediaChoices(
            imageChoices.Assets,
            imageChoices.Groups,
            videoChoices.Assets,
            videoChoices.Groups);
    }
}
