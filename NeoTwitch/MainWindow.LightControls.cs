using System.Windows;
using NeoTwitch.Models;
using NeoTwitch.Services;
using NeoTwitch.Services.Lights;
using WpfMessageBox = System.Windows.MessageBox;
using WpfBorder = System.Windows.Controls.Border;
using WpfButton = System.Windows.Controls.Button;
using WpfCheckBox = System.Windows.Controls.CheckBox;
using WpfComboBox = System.Windows.Controls.ComboBox;
using WpfDockPanel = System.Windows.Controls.DockPanel;
using WpfGrid = System.Windows.Controls.Grid;
using WpfItemsControl = System.Windows.Controls.ItemsControl;
using WpfListBox = System.Windows.Controls.ListBox;
using WpfPath = System.Windows.Shapes.Path;
using WpfSlider = System.Windows.Controls.Slider;
using WpfStackPanel = System.Windows.Controls.StackPanel;
using WpfTextBlock = System.Windows.Controls.TextBlock;
using WpfTextBox = System.Windows.Controls.TextBox;

namespace NeoTwitch;

public partial class MainWindow
{
    private WpfStackPanel StripEditorPanel => LightsView.StripEditorPanel;
    private WpfListBox StripsList => LightsView.StripsList;
    private WpfTextBox StripNameBox => LightsView.StripNameBox;
    private WpfTextBox StripPinBox => LightsView.StripPinBox;
    private WpfTextBox StripLedCountBox => LightsView.StripLedCountBox;
    private WpfCheckBox BackgroundEnabledCheck => LightsView.BackgroundEnabledCheck;
    private WpfTextBox BackgroundPinsBox => LightsView.BackgroundPinsBox;
    private WpfStackPanel BackgroundPatternGrid => LightsView.BackgroundPatternGrid;
    private WpfComboBox BackgroundPatternBox => LightsView.BackgroundPatternBox;
    private WpfButton BackgroundPatternSolidTileButton => LightsView.BackgroundPatternSolidTileButton;
    private WpfButton BackgroundPatternPulseTileButton => LightsView.BackgroundPatternPulseTileButton;
    private WpfButton BackgroundPatternRainbowTileButton => LightsView.BackgroundPatternRainbowTileButton;
    private WpfButton BackgroundPatternChaseTileButton => LightsView.BackgroundPatternChaseTileButton;
    private WpfButton BackgroundPatternTheaterTileButton => LightsView.BackgroundPatternTheaterTileButton;
    private WpfButton BackgroundPatternSparkleTileButton => LightsView.BackgroundPatternSparkleTileButton;
    private WpfButton BackgroundPatternRaveTileButton => LightsView.BackgroundPatternRaveTileButton;
    private WpfGrid BackgroundColorOptionsGrid => LightsView.BackgroundColorOptionsGrid;
    private WpfGrid BackgroundBrightnessPanel => LightsView.BackgroundBrightnessPanel;
    private WpfTextBlock BackgroundBrightnessValueText => LightsView.BackgroundBrightnessValueText;
    private WpfPath BackgroundBrightnessArc => LightsView.BackgroundBrightnessArc;
    private WpfSlider BackgroundBrightnessSlider => LightsView.BackgroundBrightnessSlider;
    private WpfTextBlock BackgroundPrimaryColorLabel => LightsView.BackgroundPrimaryColorLabel;
    private WpfDockPanel BackgroundPrimaryColorPanel => LightsView.BackgroundPrimaryColorPanel;
    private WpfButton BackgroundPrimaryColorButton => LightsView.BackgroundPrimaryColorButton;
    private WpfTextBox BackgroundPrimaryColorBox => LightsView.BackgroundPrimaryColorBox;
    private WpfTextBlock BackgroundSecondaryColorLabel => LightsView.BackgroundSecondaryColorLabel;
    private WpfDockPanel BackgroundSecondaryColorPanel => LightsView.BackgroundSecondaryColorPanel;
    private WpfButton BackgroundSecondaryColorButton => LightsView.BackgroundSecondaryColorButton;
    private WpfTextBox BackgroundSecondaryColorBox => LightsView.BackgroundSecondaryColorBox;
    private WpfTextBlock BackgroundTertiaryColorLabel => LightsView.BackgroundTertiaryColorLabel;
    private WpfDockPanel BackgroundTertiaryColorPanel => LightsView.BackgroundTertiaryColorPanel;
    private WpfButton BackgroundTertiaryColorButton => LightsView.BackgroundTertiaryColorButton;
    private WpfTextBox BackgroundTertiaryColorBox => LightsView.BackgroundTertiaryColorBox;
    private WpfGrid BackgroundCycleGrid => LightsView.BackgroundCycleGrid;
    private WpfTextBox BackgroundCycleValueText => LightsView.BackgroundCycleValueText;
    private WpfSlider BackgroundCycleSlider => LightsView.BackgroundCycleSlider;
    private WpfGrid BackgroundStepGrid => LightsView.BackgroundStepGrid;
    private WpfTextBox BackgroundStepValueText => LightsView.BackgroundStepValueText;
    private WpfSlider BackgroundStepSlider => LightsView.BackgroundStepSlider;
    private WpfBorder BackgroundLedPreviewPanel => LightsView.BackgroundLedPreviewPanel;
    private WpfItemsControl BackgroundLedPreviewList => LightsView.BackgroundLedPreviewList;
    private WpfButton ApplyArduinoBackgroundButton => LightsView.ApplyArduinoBackgroundButton;
    private WpfTextBlock LightsArduinoDeviceText => LightsView.LightsArduinoDeviceText;
    private WpfTextBlock LightsArduinoPortText => LightsView.LightsArduinoPortText;
    private WpfTextBlock LightsArduinoLedCountText => LightsView.LightsArduinoLedCountText;
    private WpfTextBlock LightsArduinoPinsText => LightsView.LightsArduinoPinsText;

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
