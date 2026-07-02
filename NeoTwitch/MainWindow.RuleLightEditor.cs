using System.Windows;
using System.Windows.Controls;
using NeoTwitch.Models;
using NeoTwitch.Services.Lights;

namespace NeoTwitch;

public partial class MainWindow
{
    internal void TargetPinsChoiceBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_initializingComponent || _loadingRule)
        {
            return;
        }

        TargetPinsBox.Text = TargetPinsChoiceBox.SelectedValue?.ToString() ?? "";
        if (SaveCurrentRuleFromFields())
        {
            UpdateRuleDirtyStateFromSnapshot();
        }
    }

    private void SelectRuleLightPreset(object? parameter)
    {
        if (parameter?.ToString() is not { Length: > 0 } preset)
        {
            return;
        }

        var values = LightControlInputService.GetRulePreset(preset);
        BrightnessSlider.Value = values.Brightness;
        DurationSlider.Value = values.DurationMs;
        CycleSlider.Value = values.CycleMs;
        StepSlider.Value = values.StepMs;

        if (SaveCurrentRuleFromFields())
        {
            UpdateRuleDirtyStateFromSnapshot();
        }
    }

    private void AdjustRuleLightValue(object? parameter)
    {
        if (_initializingComponent || _loadingRule)
        {
            return;
        }

        if (!LightControlInputService.TryParseDelta(parameter?.ToString(), out var delta))
        {
            return;
        }

        var slider = delta.Target switch
        {
            "Brightness" => BrightnessSlider,
            "Duration" => DurationSlider,
            "Cycle" => CycleSlider,
            "Step" => StepSlider,
            _ => null
        };

        if (slider is null)
        {
            return;
        }

        slider.Value = LightControlInputService.AdjustValue(slider.Value, delta.Amount, slider.Minimum, slider.Maximum);
        if (SaveCurrentRuleFromFields())
        {
            UpdateRuleDirtyStateFromSnapshot();
        }

        UpdateSliderLabels();
        UpdateRuleLedPreviewFrame();
    }

    internal void LightNumberBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_initializingComponent || _loadingRule || _updatingLightValueFields || sender is not System.Windows.Controls.TextBox textBox)
        {
            return;
        }

        var slider = ReferenceEquals(textBox, DurationValueText)
            ? DurationSlider
            : ReferenceEquals(textBox, CycleValueText)
                ? CycleSlider
                : ReferenceEquals(textBox, StepValueText)
                    ? StepSlider
                    : null;

        if (slider is null || !LightControlInputService.TryParseSliderText(textBox.Text, slider.Minimum, slider.Maximum, out var value))
        {
            return;
        }

        slider.Value = value;
        if (SaveCurrentRuleFromFields())
        {
            UpdateRuleDirtyStateFromSnapshot();
        }

        UpdateSliderLabels();
        UpdateRuleLedPreviewFrame();
    }

    private void RefreshRulePinChoices()
    {
        if (TargetPinsChoiceBox is null)
        {
            return;
        }

        var choices = RulePinChoiceService.BuildChoices(_config.LedStrips, TargetPinsBox.Text);

        var wasLoading = _loadingRule;
        _loadingRule = true;
        try
        {
            _alertsViewModel.UpdateTargetPinChoices(choices.Options);
            TargetPinsChoiceBox.SelectedValue = choices.CurrentPins;

            if (TargetPinsChoiceBox.SelectedIndex < 0)
            {
                TargetPinsChoiceBox.SelectedIndex = 0;
            }
        }
        finally
        {
            _loadingRule = wasLoading;
        }
    }

    private void SelectRuleLightPattern(object? parameter)
    {
        if (!TryParseEnumParameter<LightPattern>(parameter, out var pattern))
        {
            return;
        }

        PatternBox.SelectedValue = pattern;
        UpdatePatternTileSelection();
        UpdateRuleLedPreviewFrame();
    }

    internal void RuleLedPreviewPanel_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        UpdateRuleLedPreviewTimerState();
    }
}
