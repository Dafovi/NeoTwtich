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
        var sendObsScene = editor.SendObsScene;
        var selectedObsSceneName = editor.ObsSceneName;
        var returnObsScene = editor.ObsReturnToPreviousScene;
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
        var pattern = editor.Pattern;

        return new RuleOptionVisibilityInput(
            kind,
            arduinoAvailable,
            useLights,
            playAudio,
            editor.AudioSourceMode,
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
            editor.SendObsImage,
            editor.ObsImageSourceMode,
            imageChoices.HasAssets,
            imageChoices.HasGroups,
            editor.SendObsVideo,
            editor.ObsVideoSourceMode,
            videoChoices.HasAssets,
            videoChoices.HasGroups,
            pattern);
    }
}
