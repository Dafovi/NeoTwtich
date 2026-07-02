using NeoTwitch.Services;
using NeoTwitch.Models;
using NeoTwitch.Services.Text;
using NeoTwitch.Shared;
using NeoTwitch.ViewModels.Library;

namespace NeoTwitch;

public partial class MainWindow
{
    private void LoadConnectionConfigIntoUi()
    {
        _connectionsViewModel.LoadTwitchConfig(_config);
        _connectionsViewModel.LoadArduinoConfig(_config);
        _connectionsViewModel.LoadAlexaConfig(_config);
        _connectionsViewModel.LoadObsConnectionConfig(_config);
        ObsOverlayWidthBox.Text = _config.Obs.OverlayWidth.ToString();
        ObsOverlayHeightBox.Text = _config.Obs.OverlayHeight.ToString();
        ObsOverlayMediaWidthBox.Text = _config.Obs.OverlayMediaWidth.ToString();
        ObsOverlayMediaHeightBox.Text = _config.Obs.OverlayMediaHeight.ToString();
        ObsOverlayPositionBox.SelectedValue = _config.Obs.OverlayPositionMode;
        ObsOverlayXBox.Text = _config.Obs.OverlayX.ToString();
        ObsOverlayYBox.Text = _config.Obs.OverlayY.ToString();
    }

    private void LoadGlobalPreferencesIntoUi()
    {
        _settingsViewModel.LoadPreferences(_config);
        _audioLibraryViewModel.SetVolume(_config.AlertVolumePercent, notify: false);
        _videoLibraryViewModel.SetVolume(_config.VideoVolumePercent, notify: false);
    }

    private void LoadQueueConfigIntoUi()
    {
    }

    private void LoadBackgroundConfigIntoUi()
    {
        _lightsViewModel.LoadBackground(_config);
        BackgroundAlexaEnabledCheck.IsChecked = _config.BackgroundAlexaEnabled;
        BackgroundAlexaTurnOffAfterEventCheck.IsChecked = _config.BackgroundAlexaTurnOffAfterEvent;
        BackgroundAlexaOnEventBox.Text = _config.BackgroundAlexaOnEventName;
        BackgroundAlexaOffEventBox.Text = _config.BackgroundAlexaOffEventName;
    }

    private void BindConfigCollectionsIntoUi()
    {
        _alertsViewModel.SetRulesSource(_config.Rules);
        _alertsViewModel.UpdateEditorChoices(
            _eventOptions,
            _patternOptions,
            _config.AudioLibrary,
            _config.AudioGroups,
            _obsSceneChoices,
            _obsMediaKindOptions,
            _mediaSourceModeOptions);
        _audioLibraryViewModel.SetNewAssetChoices(AudioGroupChoices, AudioAlertChoices);
        _imageLibraryViewModel.SetNewAssetChoices(ImageGroupChoices);
        _videoLibraryViewModel.SetNewAssetChoices(VideoGroupChoices);
        _lightsViewModel.SetLedStripsSource(_config.LedStrips);
    }

    private void LoadSettingsMetadataIntoUi()
    {
        _settingsViewModel.UpdateMetadata(
            _settingsStore.SettingsPath,
            _text.Format(UiTextKeys.SettingsAutomaticBackupsText, _settingsStore.BackupDirectory),
            $"V{NeoTwitchProduct.CurrentVersionText}");
        UpdateCloseBehaviorCards();
    }

    private void RefreshLoadedConfigUi()
    {
        UpdateBackgroundOptionVisibility();
        UpdateBackgroundPatternTileSelection();
        UpdateBackgroundLedPreviewFrame();
        RefreshAudioLibraryView();
        UpdateAudioFilterButtons();
        UpdateLightsArduinoStatus();
        ApplyBackgroundOutputMode();
        UpdateAlexaStatusText();
        UpdateObsStatusText();
        UpdateSensitiveFieldVisibility();
        ApplyTheme();
        UpdateNavigationButtons();
        UpdateStatusText();
        RefreshMediaLibraryView(MediaLibraryKind.Image);
        RefreshMediaLibraryView(MediaLibraryKind.Video);
    }
}
