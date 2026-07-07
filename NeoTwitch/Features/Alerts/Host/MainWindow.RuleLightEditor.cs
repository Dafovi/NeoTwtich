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

        var isVirtual = preset.StartsWith("Virtual:", StringComparison.OrdinalIgnoreCase);
        if (isVirtual)
        {
            preset = preset["Virtual:".Length..];
        }

        var values = LightControlInputService.GetRulePreset(preset);
        if (isVirtual)
        {
            _alertsViewModel.Editor.VirtualLightsBrightness = values.Brightness;
            _alertsViewModel.Editor.VirtualLightsDurationMs = values.DurationMs;
            _alertsViewModel.Editor.VirtualLightsCycleMs = values.CycleMs;
            _alertsViewModel.Editor.VirtualLightsStepMs = values.StepMs;
        }
        else
        {
            _alertsViewModel.Editor.Brightness = values.Brightness;
            _alertsViewModel.Editor.DurationMs = values.DurationMs;
            _alertsViewModel.Editor.CycleMs = values.CycleMs;
            _alertsViewModel.Editor.StepMs = values.StepMs;
        }

        if (SaveCurrentRuleFromFields())
        {
            UpdateRuleDirtyStateFromSnapshot();
        }

        UpdateSliderLabels();
        UpdateRuleLedPreviewFrame();
        UpdateVirtualRuleLedPreviewFrame();
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

        var isVirtual = delta.Target.StartsWith("Virtual", StringComparison.OrdinalIgnoreCase);
        var target = isVirtual ? delta.Target["Virtual".Length..] : delta.Target;

        var current = (isVirtual, target) switch
        {
            (false, "Brightness") => _alertsViewModel.Editor.Brightness,
            (false, "Duration") => _alertsViewModel.Editor.DurationMs,
            (false, "Cycle") => _alertsViewModel.Editor.CycleMs,
            (false, "Step") => _alertsViewModel.Editor.StepMs,
            (true, "Brightness") => _alertsViewModel.Editor.VirtualLightsBrightness,
            (true, "Duration") => _alertsViewModel.Editor.VirtualLightsDurationMs,
            (true, "Cycle") => _alertsViewModel.Editor.VirtualLightsCycleMs,
            (true, "Step") => _alertsViewModel.Editor.VirtualLightsStepMs,
            _ => double.NaN
        };

        if (double.IsNaN(current) || !LightControlInputService.TryGetRuleRange(target, out var range))
        {
            return;
        }

        var value = LightControlInputService.AdjustValue(current, delta.Amount, range.Minimum, range.Maximum);
        switch (isVirtual, target)
        {
            case (false, "Brightness"):
                _alertsViewModel.Editor.Brightness = value;
                break;
            case (false, "Duration"):
                _alertsViewModel.Editor.DurationMs = value;
                break;
            case (false, "Cycle"):
                _alertsViewModel.Editor.CycleMs = value;
                break;
            case (false, "Step"):
                _alertsViewModel.Editor.StepMs = value;
                break;
            case (true, "Brightness"):
                _alertsViewModel.Editor.VirtualLightsBrightness = value;
                break;
            case (true, "Duration"):
                _alertsViewModel.Editor.VirtualLightsDurationMs = value;
                break;
            case (true, "Cycle"):
                _alertsViewModel.Editor.VirtualLightsCycleMs = value;
                break;
            case (true, "Step"):
                _alertsViewModel.Editor.VirtualLightsStepMs = value;
                break;
        }

        if (SaveCurrentRuleFromFields())
        {
            UpdateRuleDirtyStateFromSnapshot();
        }

        UpdateSliderLabels();
        UpdateRuleLedPreviewFrame();
        UpdateVirtualRuleLedPreviewFrame();
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
                    : ReferenceEquals(textBox, VirtualDurationValueText)
                        ? VirtualDurationSlider
                        : ReferenceEquals(textBox, VirtualCycleValueText)
                            ? VirtualCycleSlider
                            : ReferenceEquals(textBox, VirtualStepValueText)
                                ? VirtualStepSlider
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
        UpdateVirtualRuleLedPreviewFrame();
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

            UpdateRuleOptionVisibility();
            UpdateVirtualRuleLedPreviewFrame();
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
