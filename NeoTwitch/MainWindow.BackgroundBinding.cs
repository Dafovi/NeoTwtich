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
        _config.BackgroundEnabled = _lightsViewModel.BackgroundEnabled;
        _config.BackgroundAlexaEnabled = _alexaViewModel.BackgroundEnabled;
        _config.BackgroundAlexaTurnOffAfterEvent = _alexaViewModel.BackgroundTurnOffAfterEvent;
        _config.BackgroundAlexaOnEventName = NormalizeEventName(_alexaViewModel.BackgroundOnEventName, "luz_encendida");
        _config.BackgroundAlexaOffEventName = NormalizeEventName(_alexaViewModel.BackgroundOffEventName, "luz_apagada");
        _config.BackgroundTargetPins = string.Join(", ", LightCommand.ParsePins(_lightsViewModel.BackgroundTargetPins));
        _config.BackgroundPattern = _lightsViewModel.BackgroundPattern;
        _config.BackgroundPrimaryColor = LightCommand.NormalizeColor(_lightsViewModel.BackgroundPrimaryColor);
        _config.BackgroundSecondaryColor = LightCommand.NormalizeColor(_lightsViewModel.BackgroundSecondaryColor);
        _config.BackgroundTertiaryColor = LightCommand.NormalizeColor(_lightsViewModel.BackgroundTertiaryColor);
        _config.BackgroundBrightness = (int)Math.Round(_lightsViewModel.BackgroundBrightness);
        _config.BackgroundCycleMs = (int)Math.Round(_lightsViewModel.BackgroundCycleMs);
        _config.BackgroundStepMs = (int)Math.Round(_lightsViewModel.BackgroundStepMs);

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
        var alexaEnabled = _alexaViewModel.BackgroundEnabled;
        var alexaTurnOffAfterEvent = _alexaViewModel.BackgroundTurnOffAfterEvent;
        var alexaAvailable = _config.Alexa.IsConfigured;
        var pattern = _lightsViewModel.BackgroundPattern;

        var visibility = OptionVisibilityService.ResolveBackground(new BackgroundOptionVisibilityInput(
            arduinoAvailable,
            _lightsViewModel.BackgroundEnabled,
            alexaAvailable,
            alexaEnabled,
            alexaTurnOffAfterEvent,
            pattern));

        UiVisibilityService.SetVisible(visibility.ShowAlexaControls, BackgroundAlexaEnabledCheck, BackgroundAlexaTurnOffAfterEventCheck, StopAlexaBackgroundButton);
        UiVisibilityService.SetVisible(visibility.ShowAlexaUnavailable, AlexaBackgroundUnavailableText);
        UiVisibilityService.SetVisible(visibility.ShowAlexaEvents, BackgroundAlexaEventsGrid, ApplyAlexaBackgroundButton);
        UiVisibilityService.SetVisible(visibility.ShowArduinoEnabled, BackgroundEnabledCheck);
        UiVisibilityService.SetVisible(visibility.ShowArduinoBackground, BackgroundPatternGrid, BackgroundLedPreviewPanel, ApplyArduinoBackgroundButton);
        UiVisibilityService.SetVisible(visibility.ShowColorOptions, BackgroundColorOptionsGrid);
        UiVisibilityService.SetVisible(visibility.ShowBrightness, BackgroundBrightnessPanel);
        UiVisibilityService.SetVisible(visibility.ShowPrimaryColor, BackgroundPrimaryColorLabel, BackgroundPrimaryColorPanel);
        UiVisibilityService.SetVisible(visibility.ShowSecondaryColor, BackgroundSecondaryColorLabel, BackgroundSecondaryColorPanel);
        UiVisibilityService.SetVisible(visibility.ShowTertiaryColor, BackgroundTertiaryColorLabel, BackgroundTertiaryColorPanel);
        UiVisibilityService.SetVisible(visibility.ShowCycle, BackgroundCycleGrid);
        UiVisibilityService.SetVisible(visibility.ShowStep, BackgroundStepGrid);
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
