using NeoTwitch.Models;
using NeoTwitch.Services;

namespace NeoTwitch;

public partial class MainWindow
{
    private void AddStrip()
    {
        var strip = ConfigurationItemFactory.CreateLedStrip(_config.LedStrips, _text);
        _config.LedStrips.Add(strip);
        _lightsViewModel.SelectedStrip = strip;
        SaveConfig();
    }

    private void DuplicateStrip()
    {
        if (_lightsViewModel.SelectedStrip is not LedStripConfig strip)
        {
            return;
        }

        var copy = strip.Duplicate();
        _config.LedStrips.Add(copy);
        _lightsViewModel.SelectedStrip = copy;
        SaveConfig();
    }

    private void RemoveStrip()
    {
        if (_lightsViewModel.SelectedStrip is not LedStripConfig strip)
        {
            return;
        }

        if (_config.LedStrips.Count == 1)
        {
            _dialog.ShowInformation("Luces de fondo", "Deja al menos una tira configurada.");
            return;
        }

        var index = _config.LedStrips.IndexOf(strip);
        _config.LedStrips.Remove(strip);
        _lightsViewModel.SelectedStrip = _config.LedStrips[Math.Clamp(index - 1, 0, _config.LedStrips.Count - 1)];
        SaveConfig();
    }

    private async void ApplyArduinoBackground()
    {
        SaveGlobalSettingsFromFields();
        SaveCurrentStripFromFields();
        SaveBackgroundFromFields();
        SaveConfig();
        await ApplyArduinoBackgroundAsync();
    }

    private async void StopArduinoBackground()
    {
        SaveGlobalSettingsFromFields();
        SaveBackgroundFromFields();
        SaveConfig();
        await StopLightsAsync(LightCommand.ResolveTargets(_config, ""));
    }
}
