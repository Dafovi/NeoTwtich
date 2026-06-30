using System.Windows;
using System.Windows.Controls;
using NeoTwitch.Models;
using NeoTwitch.Services.Lights;

namespace NeoTwitch;

public partial class MainWindow
{
    private void SelectBackgroundPattern(object? parameter)
    {
        if (!TryParseEnumParameter<LightPattern>(parameter, out var pattern))
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

    internal void BackgroundLedPreviewPanel_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        UpdateBackgroundLedPreviewTimerState();
    }
}
