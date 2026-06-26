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

        var kind = RuleObsMediaKindBox.SelectedValue is ObsMediaKind selectedKind
            ? selectedKind
            : ObsMediaKind.Image;

        var choices = RuleObsMediaChoiceService.Resolve(
            kind,
            _config.ImageLibrary,
            _config.VideoLibrary,
            _config.ImageGroups,
            _config.VideoGroups);

        RuleObsMediaAssetBox.ItemsSource = choices.Assets;
        RuleObsMediaGroupBox.ItemsSource = choices.Groups;

        RuleObsMediaAssetBox.DisplayMemberPath = nameof(MediaAssetConfig.DisplayName);
        RuleObsMediaAssetBox.SelectedValuePath = nameof(MediaAssetConfig.Id);
        RuleObsMediaGroupBox.DisplayMemberPath = nameof(MediaGroupConfig.Name);
        RuleObsMediaGroupBox.SelectedValuePath = nameof(MediaGroupConfig.Id);
    }
}
