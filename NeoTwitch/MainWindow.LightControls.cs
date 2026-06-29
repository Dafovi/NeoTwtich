using System.Windows;
using NeoTwitch.Models;
using NeoTwitch.Services;
using WpfMessageBox = System.Windows.MessageBox;

namespace NeoTwitch;

public partial class MainWindow
{
    private void AddStrip()
    {
        var strip = ConfigurationItemFactory.CreateLedStrip(_config.LedStrips, _text);
        _config.LedStrips.Add(strip);
        StripsList.SelectedItem = strip;
        SaveConfig();
    }

    private void DuplicateStrip()
    {
        if (StripsList.SelectedItem is not LedStripConfig strip)
        {
            return;
        }

        var copy = strip.Duplicate();
        _config.LedStrips.Add(copy);
        StripsList.SelectedItem = copy;
        SaveConfig();
    }

    private void RemoveStrip()
    {
        if (StripsList.SelectedItem is not LedStripConfig strip)
        {
            return;
        }

        if (_config.LedStrips.Count == 1)
        {
            WpfMessageBox.Show(this, "Deja al menos una tira configurada.", "Luces de fondo", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var index = StripsList.SelectedIndex;
        _config.LedStrips.Remove(strip);
        StripsList.SelectedIndex = Math.Clamp(index - 1, 0, _config.LedStrips.Count - 1);
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
