using System.Windows.Media;
using NeoTwitch.Models;
using NeoTwitch.Services;

namespace NeoTwitch;

public partial class MainWindow
{
    private void PickRuleLightColor(object? parameter)
    {
        PickColor(parameter?.ToString() switch
        {
            "Primary" => PrimaryColorBox,
            "Secondary" => SecondaryColorBox,
            "Tertiary" => TertiaryColorBox,
            "VirtualPrimary" => VirtualPrimaryColorBox,
            "VirtualSecondary" => VirtualSecondaryColorBox,
            "VirtualTertiary" => VirtualTertiaryColorBox,
            _ => null
        });
    }

    private void PickBackgroundLightColor(object? parameter)
    {
        PickColor(parameter?.ToString() switch
        {
            "Primary" => BackgroundPrimaryColorBox,
            "Secondary" => BackgroundSecondaryColorBox,
            "Tertiary" => BackgroundTertiaryColorBox,
            _ => null
        });
    }

    private void UpdateColorButtons()
    {
        PrimaryColorButton.Background = ToBrush(_alertsViewModel.Editor.PrimaryColor);
        SecondaryColorButton.Background = ToBrush(_alertsViewModel.Editor.SecondaryColor);
        TertiaryColorButton.Background = ToBrush(_alertsViewModel.Editor.TertiaryColor);
        VirtualPrimaryColorButton.Background = ToBrush(_alertsViewModel.Editor.VirtualLightsPrimaryColor);
        VirtualSecondaryColorButton.Background = ToBrush(_alertsViewModel.Editor.VirtualLightsSecondaryColor);
        VirtualTertiaryColorButton.Background = ToBrush(_alertsViewModel.Editor.VirtualLightsTertiaryColor);
        BackgroundPrimaryColorButton.Background = ToBrush(_lightsViewModel.BackgroundPrimaryColor);
        BackgroundSecondaryColorButton.Background = ToBrush(_lightsViewModel.BackgroundSecondaryColor);
        BackgroundTertiaryColorButton.Background = ToBrush(_lightsViewModel.BackgroundTertiaryColor);
    }

    private static SolidColorBrush ToBrush(string color)
    {
        try
        {
            return new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(LightCommand.NormalizeColor(color)));
        }
        catch
        {
            return new SolidColorBrush(Colors.White);
        }
    }

    private void PickColor(System.Windows.Controls.TextBox? target)
    {
        if (target is null)
        {
            return;
        }

        var dialog = new Views.ColorPickerDialog(target.Text, ThemeModeService.ResolveDarkMode(_config.ThemeMode), BuildRecentColorPalette())
        {
            Owner = this
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        target.Text = dialog.SelectedColorHex;
        RememberRecentColor(dialog.SelectedColorHex);
        SaveConfig();
    }

    private IEnumerable<string> BuildRecentColorPalette()
    {
        foreach (var color in _config.RecentColors)
        {
            yield return color;
        }

        yield return _alertsViewModel.Editor.PrimaryColor;
        yield return _alertsViewModel.Editor.SecondaryColor;
        yield return _alertsViewModel.Editor.TertiaryColor;
        yield return _alertsViewModel.Editor.VirtualLightsPrimaryColor;
        yield return _alertsViewModel.Editor.VirtualLightsSecondaryColor;
        yield return _alertsViewModel.Editor.VirtualLightsTertiaryColor;
        yield return _lightsViewModel.BackgroundPrimaryColor;
        yield return _lightsViewModel.BackgroundSecondaryColor;
        yield return _lightsViewModel.BackgroundTertiaryColor;

        foreach (var rule in _config.Rules.Take(12))
        {
            yield return rule.PrimaryColor;
            yield return rule.SecondaryColor;
            yield return rule.TertiaryColor;
            yield return rule.VirtualLightsPrimaryColor;
            yield return rule.VirtualLightsSecondaryColor;
            yield return rule.VirtualLightsTertiaryColor;
        }
    }

    private void RememberRecentColor(string color)
    {
        var normalized = LightCommand.NormalizeColor(color);
        var existing = _config.RecentColors
            .FirstOrDefault(item => string.Equals(item, normalized, StringComparison.OrdinalIgnoreCase));

        if (existing is not null)
        {
            _config.RecentColors.Remove(existing);
        }

        _config.RecentColors.Insert(0, normalized);

        while (_config.RecentColors.Count > ApplicationLimits.MaxRecentColors)
        {
            _config.RecentColors.RemoveAt(_config.RecentColors.Count - 1);
        }
    }
}
