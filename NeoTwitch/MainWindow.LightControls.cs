using System.Windows;
using NeoTwitch.Models;
using NeoTwitch.Services;
using WpfMessageBox = System.Windows.MessageBox;

namespace NeoTwitch;

public partial class MainWindow
{
    internal void AddStripButton_Click(object sender, RoutedEventArgs e)
    {
        var strip = ConfigurationItemFactory.CreateLedStrip(_config.LedStrips);
        _config.LedStrips.Add(strip);
        StripsList.SelectedItem = strip;
        SaveConfig();
    }

    internal void DuplicateStripButton_Click(object sender, RoutedEventArgs e)
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

    internal void RemoveStripButton_Click(object sender, RoutedEventArgs e)
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

    internal async void ApplyArduinoBackgroundButton_Click(object sender, RoutedEventArgs e)
    {
        SaveGlobalSettingsFromFields();
        SaveCurrentStripFromFields();
        SaveBackgroundFromFields();
        SaveConfig();
        await ApplyArduinoBackgroundAsync();
    }

    internal async void StopArduinoBackgroundButton_Click(object sender, RoutedEventArgs e)
    {
        SaveGlobalSettingsFromFields();
        SaveBackgroundFromFields();
        SaveConfig();
        await StopLightsAsync(LightCommand.ResolveTargets(_config, ""));
    }
}
