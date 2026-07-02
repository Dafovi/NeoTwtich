using NeoTwitch.Models;
using NeoTwitch.Services;
using NeoTwitch.ViewModels.Library;
using NeoTwitch.ViewModels.Obs;
using NeoTwitch.ViewModels.Ui;

namespace NeoTwitch;

public partial class MainWindow
{
    private void InitializeRulesBinding()
    {
        _alertsViewModel.SetRulesSource(_config.Rules);
    }

    private void InitializeRuleOptionSources()
    {
        _alertsViewModel.UpdateEditorChoices(
            _eventOptions,
            _patternOptions,
            _config.AudioLibrary,
            _config.AudioGroups,
            _obsSceneChoices,
            _obsMediaKindOptions,
            _mediaSourceModeOptions);

    }

    private void InitializeLibraryOptionSources()
    {
        _audioLibraryViewModel.SetNewAssetChoices(AudioGroupChoices, AudioAlertChoices);
        _imageLibraryViewModel.SetNewAssetChoices(ImageGroupChoices);
        _videoLibraryViewModel.SetNewAssetChoices(VideoGroupChoices);
    }

    private void InitializeBackgroundOptionSources()
    {
        _lightsViewModel.UpdateBackgroundPatternChoices(_patternOptions);
    }

    private void InitializeConnectionOptionSources()
    {
        _settingsViewModel.UpdateThemeModeChoices(_themeModeOptions);
        _lightsViewModel.SetLedStripsSource(_config.LedStrips);
    }
}
