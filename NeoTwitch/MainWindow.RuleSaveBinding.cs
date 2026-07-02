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

        var pattern = PatternBox.SelectedValue is LightPattern selectedPattern
            ? selectedPattern
            : LightPattern.Pulse;

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
                editor.UseLights,
                editor.PlayAudio,
                _ruleAudioMode,
                RuleAudioAssetBox.SelectedValue as string ?? "",
                RuleAudioGroupBox.SelectedValue as string ?? "",
                pattern,
                TargetPinsBox.Text,
                PrimaryColorBox.Text,
                SecondaryColorBox.Text,
                TertiaryColorBox.Text,
                BrightnessSlider.Value,
                DurationSlider.Value,
                CycleSlider.Value,
                StepSlider.Value),
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
