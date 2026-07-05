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

        var kind = _alertsViewModel.Editor.ObsMediaKind;

        var choices = RuleObsMediaChoiceService.Resolve(
            kind,
            _config.ImageLibrary,
            _config.VideoLibrary,
            _config.ImageGroups,
            _config.VideoGroups);

        _alertsViewModel.UpdateObsMediaChoices(choices.Assets, choices.Groups);
    }
}
