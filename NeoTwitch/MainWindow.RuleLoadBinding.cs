using NeoTwitch.Models;
using NeoTwitch.Services.Lights;

namespace NeoTwitch;

public partial class MainWindow
{
    private void LoadSelectedRuleIntoUi()
    {
        _loadingRule = true;

        try
        {
            _alertsViewModel.SetEditorEnabled(_alertsViewModel.SelectedRule is EventRule);

            if (_alertsViewModel.SelectedRule is not EventRule rule)
            {
                _editingRule = null;
                _loadedRuleSnapshot = null;
                _alertsViewModel.Editor.Clear();
                SetRuleDirtyState(false);
                return;
            }

            _editingRule = rule;
            _alertsViewModel.Editor.LoadBasicFields(rule);
            UpdateEventKindTileSelection();
            RefreshRuleObsMediaChoices();
            _ruleAudioMode = rule.AudioSourceMode;
            RuleAudioAssetBox.SelectedValue = rule.AudioAssetId;
            RuleAudioGroupBox.SelectedValue = rule.AudioGroupId;
            PatternBox.SelectedValue = rule.Pattern;
            TargetPinsBox.Text = rule.TargetPins;
            RefreshRulePinChoices();
            PrimaryColorBox.Text = LightCommand.NormalizeColor(rule.PrimaryColor);
            SecondaryColorBox.Text = LightCommand.NormalizeColor(rule.SecondaryColor);
            TertiaryColorBox.Text = LightCommand.NormalizeColor(rule.TertiaryColor);
            BrightnessSlider.Value = rule.Brightness;
            DurationSlider.Value = rule.DurationMs;
            CycleSlider.Value = rule.CycleMs;
            StepSlider.Value = rule.StepMs;
            UpdateColorButtons();
            UpdateSliderLabels();
            UpdatePatternTileSelection();
            UpdateRuleObsMediaModeSelection();
            UpdateRuleLedPreviewFrame();
            CaptureCurrentRuleSnapshot();
            SetRuleDirtyState(false);
        }
        finally
        {
            _loadingRule = false;
            UpdateRuleOptionVisibility();
            UpdateRuleLedPreviewTimerState();
        }
    }
}
