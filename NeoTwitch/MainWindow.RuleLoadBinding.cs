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
            RuleEditorPanel.IsEnabled = RulesList.SelectedItem is EventRule;

            if (RulesList.SelectedItem is not EventRule rule)
            {
                _editingRule = null;
                _loadedRuleSnapshot = null;
                SetRuleDirtyState(false);
                return;
            }

            _editingRule = rule;
            RuleEnabledCheck.IsChecked = rule.IsEnabled;
            RuleNameBox.Text = rule.Name;
            EventKindBox.SelectedValue = rule.EventKind;
            UpdateEventKindTileSelection();
            RewardTitleBox.Text = rule.CustomRewardTitle;
            ChatCommandBox.Text = rule.ChatCommand;
            MinimumBitsBox.Text = rule.MinimumBits.ToString();
            ChatMessageCheck.IsChecked = rule.SendChatMessage;
            ChatMessageBox.Text = rule.ChatMessageTemplate;
            AlexaEventCheck.IsChecked = rule.SendAlexaEvent;
            ObsSceneCheck.IsChecked = rule.SendObsScene;
            RuleObsSceneBox.SelectedValue = rule.ObsSceneName;
            ObsSceneDelayBox.Text = rule.ObsSceneDelayMs.ToString();
            ObsReturnCheck.IsChecked = rule.ObsReturnToPreviousScene;
            ObsReturnDelayBox.Text = rule.ObsReturnDelayMs.ToString();
            ObsMediaCheck.IsChecked = rule.SendObsMedia;
            RuleObsMediaKindBox.SelectedValue = rule.ObsMediaKind;
            RuleObsMediaSourceModeBox.SelectedValue = rule.ObsMediaSourceMode;
            RefreshRuleObsMediaChoices();
            RuleObsMediaAssetBox.SelectedValue = rule.ObsMediaAssetId;
            RuleObsMediaGroupBox.SelectedValue = rule.ObsMediaGroupId;
            ObsMediaDurationBox.Text = rule.ObsMediaDurationMs.ToString();
            UseLightsCheck.IsChecked = rule.UseLights;
            PlayAudioCheck.IsChecked = rule.PlayAudio;
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
