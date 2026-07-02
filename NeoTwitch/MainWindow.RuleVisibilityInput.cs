using NeoTwitch.Models;
using NeoTwitch.Services.Alerts;
using NeoTwitch.Services.Ui;
using NeoTwitch.ViewModels.Library;

namespace NeoTwitch;

public partial class MainWindow
{
    private RuleOptionVisibilityInput BuildRuleOptionVisibilityInput()
    {
        var kind = _alertsViewModel.Editor.EventKind;
        var editor = _alertsViewModel.Editor;
        var arduinoAvailable = _config.ArduinoEnabled;
        var useLights = arduinoAvailable && editor.UseLights;
        var playAudio = editor.PlayAudio;
        var sendChat = editor.SendChatMessage;
        var alexaAvailable = _config.Alexa.IsConfigured;
        var sendAlexa = editor.SendAlexaEvent;
        var obsAvailable = _config.Obs.IsConfigured;
        var sendObsScene = ObsSceneCheck.IsChecked == true;
        var selectedObsSceneName = RuleObsSceneBox.SelectedValue as string ?? "";
        var returnObsScene = ObsReturnCheck.IsChecked == true;
        var sendObsMedia = ObsMediaCheck.IsChecked == true;
        var obsMediaKind = RuleObsMediaKindBox.SelectedValue is ObsMediaKind selectedObsMediaKind
            ? selectedObsMediaKind
            : ObsMediaKind.Image;
        var obsMediaSourceMode = RuleObsMediaSourceModeBox.SelectedValue is MediaSourceMode selectedObsMediaSourceMode
            ? selectedObsMediaSourceMode
            : MediaSourceMode.Single;
        var obsMediaChoices = RuleObsMediaChoiceService.Resolve(
            obsMediaKind,
            _config.ImageLibrary,
            _config.VideoLibrary,
            _config.ImageGroups,
            _config.VideoGroups);
        var pattern = PatternBox.SelectedValue is LightPattern selectedPattern
            ? selectedPattern
            : LightPattern.Pulse;

        return new RuleOptionVisibilityInput(
            kind,
            arduinoAvailable,
            useLights,
            playAudio,
            _ruleAudioMode,
            _config.AudioLibrary.Count > 0,
            _config.AudioGroups.Count > 0,
            sendChat,
            alexaAvailable,
            sendAlexa,
            obsAvailable,
            sendObsScene,
            selectedObsSceneName,
            returnObsScene,
            _obsSceneRows.Count > 0,
            sendObsMedia,
            obsMediaKind,
            obsMediaSourceMode,
            obsMediaChoices.HasAssets,
            obsMediaChoices.HasGroups,
            pattern);
    }
}
