using NeoTwitch.Models;
using NeoTwitch.Services.Lights;
using NeoTwitch.Services.Ui;
using static NeoTwitch.Services.InputValueParser;
using static NeoTwitch.Services.Text.UiTextFormatter;

namespace NeoTwitch;

public partial class MainWindow
{
    private void SaveBackgroundFromFields()
    {
        _config.BackgroundEnabled = BackgroundEnabledCheck.IsChecked == true;
        _config.BackgroundAlexaEnabled = BackgroundAlexaEnabledCheck.IsChecked == true;
        _config.BackgroundAlexaTurnOffAfterEvent = BackgroundAlexaTurnOffAfterEventCheck.IsChecked == true;
        _config.BackgroundAlexaOnEventName = NormalizeEventName(BackgroundAlexaOnEventBox.Text, "luz_encendida");
        _config.BackgroundAlexaOffEventName = NormalizeEventName(BackgroundAlexaOffEventBox.Text, "luz_apagada");
        _config.BackgroundTargetPins = string.Join(", ", LightCommand.ParsePins(BackgroundPinsBox.Text));
        _config.BackgroundPattern = BackgroundPatternBox.SelectedValue is LightPattern pattern ? pattern : LightPattern.Solid;
        _config.BackgroundPrimaryColor = LightCommand.NormalizeColor(BackgroundPrimaryColorBox.Text);
        _config.BackgroundSecondaryColor = LightCommand.NormalizeColor(BackgroundSecondaryColorBox.Text);
        _config.BackgroundTertiaryColor = LightCommand.NormalizeColor(BackgroundTertiaryColorBox.Text);
        _config.BackgroundBrightness = (int)Math.Round(BackgroundBrightnessSlider.Value);
        _config.BackgroundCycleMs = (int)Math.Round(BackgroundCycleSlider.Value);
        _config.BackgroundStepMs = (int)Math.Round(BackgroundStepSlider.Value);

        UpdateColorButtons();
        UpdateSliderLabels();
        UpdateBackgroundPatternTileSelection();
        UpdateBackgroundLedPreviewFrame();
        UpdateBackgroundOptionVisibility();
        UpdateAlexaStatusText();
    }

    private void UpdateBackgroundOptionVisibility()
    {
        var arduinoAvailable = _config.ArduinoEnabled;
        var alexaEnabled = BackgroundAlexaEnabledCheck.IsChecked == true;
        var alexaTurnOffAfterEvent = BackgroundAlexaTurnOffAfterEventCheck.IsChecked == true;
        var alexaAvailable = _config.Alexa.IsConfigured;
        var pattern = BackgroundPatternBox.SelectedValue is LightPattern selectedPattern
            ? selectedPattern
            : LightPattern.Solid;

        var visibility = OptionVisibilityService.ResolveBackground(new BackgroundOptionVisibilityInput(
            arduinoAvailable,
            BackgroundEnabledCheck.IsChecked == true,
            alexaAvailable,
            alexaEnabled,
            alexaTurnOffAfterEvent,
            pattern));

        SetVisible(visibility.ShowAlexaControls, BackgroundAlexaEnabledCheck, BackgroundAlexaTurnOffAfterEventCheck, StopAlexaBackgroundButton);
        SetVisible(visibility.ShowAlexaUnavailable, AlexaBackgroundUnavailableText);
        SetVisible(visibility.ShowAlexaEvents, BackgroundAlexaEventsGrid, ApplyAlexaBackgroundButton);
        SetVisible(visibility.ShowArduinoEnabled, BackgroundEnabledCheck);
        SetVisible(visibility.ShowArduinoBackground, BackgroundPatternGrid, BackgroundLedPreviewPanel, ApplyArduinoBackgroundButton);
        SetVisible(visibility.ShowColorOptions, BackgroundColorOptionsGrid);
        SetVisible(visibility.ShowBrightness, BackgroundBrightnessPanel);
        SetVisible(visibility.ShowPrimaryColor, BackgroundPrimaryColorLabel, BackgroundPrimaryColorPanel);
        SetVisible(visibility.ShowSecondaryColor, BackgroundSecondaryColorLabel, BackgroundSecondaryColorPanel);
        SetVisible(visibility.ShowTertiaryColor, BackgroundTertiaryColorLabel, BackgroundTertiaryColorPanel);
        SetVisible(visibility.ShowCycle, BackgroundCycleGrid);
        SetVisible(visibility.ShowStep, BackgroundStepGrid);
    }

    private void ApplyBackgroundOutputMode()
    {
        if (_initializingComponent)
        {
            return;
        }

        UpdateBackgroundOptionVisibility();
        UpdateBackgroundLedPreviewTimerState();
    }
}
