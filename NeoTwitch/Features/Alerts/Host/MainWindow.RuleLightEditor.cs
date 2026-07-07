using System.Windows;
using System.Windows.Controls;
using NeoTwitch.Models;
using NeoTwitch.Services.Lights;

namespace NeoTwitch;

public partial class MainWindow
{
    private void SelectRuleLightPreset(object? parameter)
    {
        if (parameter?.ToString() is not { Length: > 0 } preset)
        {
            return;
        }

        var values = LightControlInputService.GetRulePreset(preset);
        _alertsViewModel.Editor.Brightness = values.Brightness;
        _alertsViewModel.Editor.DurationMs = values.DurationMs;
        _alertsViewModel.Editor.CycleMs = values.CycleMs;
        _alertsViewModel.Editor.StepMs = values.StepMs;

        if (SaveCurrentRuleFromFields())
        {
            UpdateRuleDirtyStateFromSnapshot();
        }

        UpdateSliderLabels();
        UpdateRuleLedPreviewFrame();
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

        var current = delta.Target switch
        {
            "Brightness" => _alertsViewModel.Editor.Brightness,
            "Duration" => _alertsViewModel.Editor.DurationMs,
            "Cycle" => _alertsViewModel.Editor.CycleMs,
            "Step" => _alertsViewModel.Editor.StepMs,
            _ => double.NaN
        };

        if (double.IsNaN(current) || !LightControlInputService.TryGetRuleRange(delta.Target, out var range))
        {
            return;
        }

        var value = LightControlInputService.AdjustValue(current, delta.Amount, range.Minimum, range.Maximum);
        switch (delta.Target)
        {
            case "Brightness":
                _alertsViewModel.Editor.Brightness = value;
                break;
            case "Duration":
                _alertsViewModel.Editor.DurationMs = value;
                break;
            case "Cycle":
                _alertsViewModel.Editor.CycleMs = value;
                break;
            case "Step":
                _alertsViewModel.Editor.StepMs = value;
                break;
        }

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
        var choices = RulePinChoiceService.BuildChoices(_config.LedStrips, _alertsViewModel.Editor.TargetPins);

        var wasLoading = _loadingRule;
        _loadingRule = true;
        try
        {
            _alertsViewModel.UpdateTargetPinChoices(choices.Options);
            _alertsViewModel.Editor.TargetPins = choices.CurrentPins;
        }
        finally
        {
            _loadingRule = wasLoading;
        }
    }

    private void SelectRuleLightPattern(object? parameter)
    {
        var raw = parameter?.ToString() ?? "";
        if (raw.StartsWith("Virtual:", StringComparison.OrdinalIgnoreCase)
            && Enum.TryParse<LightPattern>(raw["Virtual:".Length..], out var virtualPattern))
        {
            _alertsViewModel.Editor.VirtualLightsPattern = virtualPattern;
            UpdateVirtualPatternTileSelection();
            if (SaveCurrentRuleFromFields())
            {
                UpdateRuleDirtyStateFromSnapshot();
            }

            return;
        }

        if (!TryParseEnumParameter<LightPattern>(parameter, out var pattern))
        {
            return;
        }

        _alertsViewModel.Editor.Pattern = pattern;
        UpdatePatternTileSelection();
        if (SaveCurrentRuleFromFields())
        {
            UpdateRuleDirtyStateFromSnapshot();
        }

        UpdateRuleLedPreviewFrame();
    }

    internal void RuleLedPreviewPanel_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        UpdateRuleLedPreviewTimerState();
    }
}
