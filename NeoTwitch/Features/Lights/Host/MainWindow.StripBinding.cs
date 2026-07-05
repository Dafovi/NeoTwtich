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
            _lightsViewModel.LoadSelectedStrip(_lightsViewModel.SelectedStrip);
        }
        finally
        {
            _loadingStrip = false;
            UpdateLightsArduinoStatus();
        }
    }

    private void SaveCurrentStripFromFields()
    {
        if (_loadingStrip || _lightsViewModel.SelectedStrip is not LedStripConfig strip)
        {
            return;
        }

        strip.Name = string.IsNullOrWhiteSpace(_lightsViewModel.SelectedStripName)
            ? "Tira LED"
            : _lightsViewModel.SelectedStripName.Trim();
        strip.Pin = ParseInt(_lightsViewModel.SelectedStripPinText, 6, 0, 53);
        strip.LedCount = ParseInt(_lightsViewModel.SelectedStripLedCountText, 30, 1, 600);

        _lightsViewModel.RefreshLedStrips();
        RefreshRulesView();
        UpdateLightsArduinoStatus();
        RefreshRulePinChoices();
    }
}
