using NeoTwitch.Models;
using NeoTwitch.Services.Lights;

namespace NeoTwitch;

public partial class MainWindow
{
    private void UpdateRuleLedPreviewFrame()
    {
        if (_initializingComponent)
        {
            return;
        }

        if (!Dispatcher.CheckAccess())
        {
            _ = Dispatcher.InvokeAsync(UpdateRuleLedPreviewFrame);
            return;
        }

        if (!ShouldRunRuleLedPreview())
        {
            UpdateRuleLedPreviewTimerState();
            return;
        }

        ResizeLedPreviewDots(_ruleLedPreviewDots, RuleLedPreviewPanel.ActualWidth);
        var pattern = PatternBox.SelectedValue is LightPattern selectedPattern
            ? selectedPattern
            : LightPattern.Pulse;
        var brightness = Math.Clamp(BrightnessSlider.Value / 255d, 0d, 1d);
        var primary = LedPreviewService.ParseColor(PrimaryColorBox.Text, "#14B8A6");
        var secondary = LedPreviewService.ParseColor(SecondaryColorBox.Text, "#B56CFF");
        var tertiary = LedPreviewService.ParseColor(TertiaryColorBox.Text, "#FFFFFF");
        var count = _ruleLedPreviewDots.Count;
        _ruleLedPreviewStep++;
        var frame = LedPreviewService.BuildFrame(pattern, _ruleLedPreviewStep, count, brightness, primary, secondary, tertiary, _previewRandom);

        for (var i = 0; i < count; i++)
        {
            _ruleLedPreviewDots[i] = PreviewDot(frame[i], brightness);
        }
    }

    private void SetRuleLedPreviewAll(string color)
    {
        ResizeLedPreviewDots(_ruleLedPreviewDots, RuleLedPreviewPanel.ActualWidth);
        var previewColor = LedPreviewService.ParseColor(color, "#334155");
        for (var i = 0; i < _ruleLedPreviewDots.Count; i++)
        {
            _ruleLedPreviewDots[i] = PreviewDot(previewColor, 0.08);
        }
    }

    private void UpdateRuleLedPreviewTimerState()
    {
        if (_initializingComponent || !Dispatcher.CheckAccess())
        {
            return;
        }

        var shouldRun = ShouldRunRuleLedPreview();
        if (shouldRun)
        {
            if (!_ruleLedPreviewTimer.IsEnabled)
            {
                _ruleLedPreviewTimer.Start();
            }

            return;
        }

        if (_ruleLedPreviewTimer.IsEnabled)
        {
            _ruleLedPreviewTimer.Stop();
        }

        if (UseLightsCheck.IsChecked != true || !_config.ArduinoEnabled)
        {
            SetRuleLedPreviewAll("#334155");
        }
    }

    private bool ShouldRunRuleLedPreview()
    {
        return UseLightsCheck.IsChecked == true
            && _config.ArduinoEnabled
            && MainTabs.SelectedIndex == 2
            && LightConfigurationPanel.IsExpanded
            && RuleLedPreviewPanel.IsVisible;
    }
}
