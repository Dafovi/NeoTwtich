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
        var mediaKind = RuleObsMediaKindBox.SelectedValue is ObsMediaKind selectedMediaKind
            ? selectedMediaKind
            : ObsMediaKind.Image;
        var mediaSourceMode = RuleObsMediaSourceModeBox.SelectedValue is MediaSourceMode selectedMediaSourceMode
            ? selectedMediaSourceMode
            : MediaSourceMode.Single;
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
                ChatMessageCheck.IsChecked == true,
                ChatMessageBox.Text,
                AlexaEventCheck.IsChecked == true,
                ObsSceneCheck.IsChecked == true,
                RuleObsSceneBox.SelectedValue as string ?? RuleObsSceneBox.Text,
                ObsSceneDelayBox.Text,
                ObsReturnCheck.IsChecked == true,
                ObsReturnDelayBox.Text,
                ObsMediaCheck.IsChecked == true,
                mediaKind,
                mediaSourceMode,
                RuleObsMediaAssetBox.SelectedValue as string ?? "",
                RuleObsMediaGroupBox.SelectedValue as string ?? "",
                ObsMediaDurationBox.Text,
                UseLightsCheck.IsChecked == true,
                PlayAudioCheck.IsChecked == true,
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
