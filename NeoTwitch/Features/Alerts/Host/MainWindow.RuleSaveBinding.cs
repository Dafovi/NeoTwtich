using NeoTwitch.Models;
using NeoTwitch.Services.Alerts;

namespace NeoTwitch;

public partial class MainWindow
{
    private bool SaveCurrentRuleFromFields()
    {
        if (_loadingRule
            || _editingRule is not EventRule rule
            || _alertsViewModel.SelectedRule is not EventRule selectedRule
            || !ReferenceEquals(selectedRule, rule)
            || !_config.Rules.Contains(rule)
            || !Enum.IsDefined(_alertsViewModel.Editor.EventKind))
        {
            return false;
        }

        var editor = _alertsViewModel.Editor;
        RefreshRuleObsMediaChoices();

        RuleEditorFormService.Apply(
            rule,
            new RuleEditorFormValues(
                editor.IsEnabled,
                editor.RuleNameText,
                editor.EventKind,
                editor.CustomRewardTitle,
                editor.ChatCommand,
                editor.MinimumBitsText,
                editor.SendChatMessage,
                editor.ChatMessageTemplate,
                editor.SendAlexaEvent,
                editor.SendObsScene,
                editor.ObsSceneName,
                editor.ObsSceneDelayText,
                editor.ObsReturnToPreviousScene,
                editor.ObsReturnDelayText,
                editor.SendObsMedia,
                editor.ObsMediaKind,
                editor.ObsMediaSourceMode,
                editor.ObsMediaAssetId,
                editor.ObsMediaGroupId,
                editor.ObsMediaDurationText,
                editor.SendObsImage,
                editor.ObsImageSourceMode,
                editor.ObsImageAssetId,
                editor.ObsImageGroupId,
                editor.ObsImageDurationText,
                editor.SendObsVideo,
                editor.ObsVideoSourceMode,
                editor.ObsVideoAssetId,
                editor.ObsVideoGroupId,
                editor.UseVirtualLights,
                editor.VirtualLightsToObs,
                editor.VirtualLightsToScreen,
                editor.VirtualLightsScreenId,
                editor.VirtualLightsPattern,
                editor.VirtualLightsPrimaryColor,
                editor.VirtualLightsSecondaryColor,
                editor.VirtualLightsTertiaryColor,
                editor.VirtualLightsBrightness,
                editor.VirtualLightsDurationMs,
                editor.VirtualLightsCycleMs,
                editor.VirtualLightsStepMs,
                editor.VirtualLightsObsOpacity,
                editor.VirtualLightsScreenPixelSize,
                editor.VirtualLightsScreenSaturation,
                editor.UseLights,
                editor.PlayAudio,
                editor.AudioSourceMode,
                editor.AudioAssetId,
                editor.AudioGroupId,
                editor.Pattern,
                editor.TargetPins,
                editor.PrimaryColor,
                editor.SecondaryColor,
                editor.TertiaryColor,
                editor.Brightness,
                editor.DurationMs,
                editor.CycleMs,
                editor.StepMs),
            _config.AudioLibrary,
            _text);

        UpdateColorButtons();
        UpdateSliderLabels();
        UpdatePatternTileSelection();
        UpdateRuleAudioModeSelection();
        UpdateRuleObsMediaModeSelection();
        UpdateRuleLedPreviewFrame();
        UpdateRuleOptionVisibility();
        UpdateRuleLedPreviewTimerState();
        RefreshRulesView();
        RefreshAudioLibraryView();

        return true;
    }
}
