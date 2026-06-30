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
        PrimaryColorButton.Background = ToBrush(PrimaryColorBox.Text);
        SecondaryColorButton.Background = ToBrush(SecondaryColorBox.Text);
        TertiaryColorButton.Background = ToBrush(TertiaryColorBox.Text);
        BackgroundPrimaryColorButton.Background = ToBrush(BackgroundPrimaryColorBox.Text);
        BackgroundSecondaryColorButton.Background = ToBrush(BackgroundSecondaryColorBox.Text);
        BackgroundTertiaryColorButton.Background = ToBrush(BackgroundTertiaryColorBox.Text);
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

        yield return PrimaryColorBox.Text;
        yield return SecondaryColorBox.Text;
        yield return TertiaryColorBox.Text;
        yield return BackgroundPrimaryColorBox.Text;
        yield return BackgroundSecondaryColorBox.Text;
        yield return BackgroundTertiaryColorBox.Text;

        foreach (var rule in _config.Rules.Take(12))
        {
            yield return rule.PrimaryColor;
            yield return rule.SecondaryColor;
            yield return rule.TertiaryColor;
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
