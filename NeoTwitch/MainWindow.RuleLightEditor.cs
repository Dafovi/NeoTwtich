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

    internal void LightPresetButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button button || button.Tag is not string preset)
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

    internal void LightValueButton_Click(object sender, RoutedEventArgs e)
    {
        if (_initializingComponent || _loadingRule || sender is not System.Windows.Controls.Button button)
        {
            return;
        }

        if (!LightControlInputService.TryParseDelta(button.Tag?.ToString(), out var delta))
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
            TargetPinsChoiceBox.ItemsSource = choices.Options;
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

    internal void PatternTile_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button button
            || button.Tag is not string value
            || !Enum.TryParse<LightPattern>(value, out var pattern))
        {
            return;
        }

        PatternBox.SelectedValue = pattern;
        UpdatePatternTileSelection();
        UpdateRuleLedPreviewFrame();
    }

    internal void BackgroundPatternTile_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button button
            || button.Tag is not string value
            || !Enum.TryParse<LightPattern>(value, out var pattern))
        {
            return;
        }

        BackgroundPatternBox.SelectedValue = pattern;
        UpdateBackgroundPatternTileSelection();
        UpdateBackgroundLedPreviewFrame();
    }

    internal void StripFieldChanged(object sender, RoutedEventArgs e)
    {
        if (_initializingComponent || _loadingStrip)
        {
            return;
        }

        SaveCurrentStripFromFields();
        SaveConfig();
    }

    internal void BackgroundFieldChanged(object sender, RoutedEventArgs e)
    {
        if (_initializingComponent || _loadingUi)
        {
            return;
        }

        SaveBackgroundFromFields();
        SaveConfig();
        UpdateBackgroundOptionVisibility();
        UpdateBackgroundPatternTileSelection();
        UpdateBackgroundLedPreviewFrame();
        UpdateBackgroundLedPreviewTimerState();
    }

    internal void BackgroundLightValueButton_Click(object sender, RoutedEventArgs e)
    {
        if (_initializingComponent || _loadingUi || sender is not System.Windows.Controls.Button button)
        {
            return;
        }

        if (!LightControlInputService.TryParseDelta(button.Tag?.ToString(), out var delta))
        {
            return;
        }

        var slider = delta.Target switch
        {
            "Brightness" => BackgroundBrightnessSlider,
            "Cycle" => BackgroundCycleSlider,
            "Step" => BackgroundStepSlider,
            _ => null
        };

        if (slider is null)
        {
            return;
        }

        slider.Value = LightControlInputService.AdjustValue(slider.Value, delta.Amount, slider.Minimum, slider.Maximum);
        SaveBackgroundFromFields();
        SaveConfig();
        UpdateSliderLabels();
        UpdateBackgroundLedPreviewFrame();
        UpdateBackgroundLedPreviewTimerState();
    }

    internal void BackgroundLightPresetButton_Click(object sender, RoutedEventArgs e)
    {
        if (_initializingComponent || _loadingUi || sender is not System.Windows.Controls.Button button || button.Tag is not string preset)
        {
            return;
        }

        var values = LightControlInputService.GetBackgroundPreset(preset);
        BackgroundBrightnessSlider.Value = values.Brightness;
        BackgroundCycleSlider.Value = values.CycleMs;
        BackgroundStepSlider.Value = values.StepMs;

        SaveBackgroundFromFields();
        SaveConfig();
        UpdateSliderLabels();
        UpdateBackgroundLedPreviewFrame();
        UpdateBackgroundLedPreviewTimerState();
    }

    internal void BackgroundLightNumberBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_initializingComponent || _loadingUi || _updatingLightValueFields || sender is not System.Windows.Controls.TextBox textBox)
        {
            return;
        }

        var slider = ReferenceEquals(textBox, BackgroundCycleValueText)
            ? BackgroundCycleSlider
            : ReferenceEquals(textBox, BackgroundStepValueText)
                ? BackgroundStepSlider
                : null;

        if (slider is null || !LightControlInputService.TryParseSliderText(textBox.Text, slider.Minimum, slider.Maximum, out var value))
        {
            return;
        }

        slider.Value = value;
        SaveBackgroundFromFields();
        SaveConfig();
        UpdateSliderLabels();
        UpdateBackgroundLedPreviewFrame();
        UpdateBackgroundLedPreviewTimerState();
    }

    internal void RuleLedPreviewPanel_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        UpdateRuleLedPreviewTimerState();
    }

    internal void BackgroundLedPreviewPanel_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        UpdateBackgroundLedPreviewTimerState();
    }
}
