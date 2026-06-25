using NeoTwitch.Models;
using static NeoTwitch.Services.InputValueParser;

namespace NeoTwitch;

public partial class MainWindow
{
    private void LoadSelectedStripIntoUi()
    {
        _loadingStrip = true;

        try
        {
            StripEditorPanel.IsEnabled = StripsList.SelectedItem is LedStripConfig;

            if (StripsList.SelectedItem is not LedStripConfig strip)
            {
                return;
            }

            StripNameBox.Text = strip.Name;
            StripPinBox.Text = strip.Pin.ToString();
            StripLedCountBox.Text = strip.LedCount.ToString();
        }
        finally
        {
            _loadingStrip = false;
            UpdateLightsArduinoStatus();
        }
    }

    private void SaveCurrentStripFromFields()
    {
        if (_loadingStrip || StripsList.SelectedItem is not LedStripConfig strip)
        {
            return;
        }

        strip.Name = string.IsNullOrWhiteSpace(StripNameBox.Text)
            ? "Tira LED"
            : StripNameBox.Text.Trim();
        strip.Pin = ParseInt(StripPinBox.Text, 6, 0, 53);
        strip.LedCount = ParseInt(StripLedCountBox.Text, 30, 1, 600);

        StripsList.Items.Refresh();
        RefreshRulesView();
        UpdateLightsArduinoStatus();
        RefreshRulePinChoices();
    }
}
