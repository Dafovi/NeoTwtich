using NeoTwitch.Models;
using NeoTwitch.Services.Alerts;

namespace NeoTwitch;

public partial class MainWindow
{
    private bool SaveCurrentRuleFromFields()
    {
        if (_loadingRule
            || _editingRule is not EventRule rule
            || RulesList.SelectedItem is not EventRule selectedRule
            || !ReferenceEquals(selectedRule, rule)
            || !_config.Rules.Contains(rule)
            || EventKindBox.SelectedValue is not TwitchEventKind kind
            || !Enum.IsDefined(kind))
        {
            return false;
        }

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
                RuleEnabledCheck.IsChecked == true,
                RuleNameBox.Text,
                kind,
                RewardTitleBox.Text,
                ChatCommandBox.Text,
                MinimumBitsBox.Text,
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
            _config.AudioLibrary);

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
