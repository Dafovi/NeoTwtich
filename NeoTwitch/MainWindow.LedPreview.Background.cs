using NeoTwitch.Models;
using NeoTwitch.Services.Lights;

namespace NeoTwitch;

public partial class MainWindow
{
    private void UpdateBackgroundLedPreviewFrame()
    {
        if (_initializingComponent)
        {
            return;
        }

        if (!Dispatcher.CheckAccess())
        {
            _ = Dispatcher.InvokeAsync(UpdateBackgroundLedPreviewFrame);
            return;
        }

        if (!ShouldRunBackgroundLedPreview())
        {
            UpdateBackgroundLedPreviewTimerState();
            return;
        }

        ResizeLedPreviewDots(_backgroundLedPreviewDots, BackgroundLedPreviewPanel.ActualWidth);
        var pattern = BackgroundPatternBox.SelectedValue is LightPattern selectedPattern
            ? selectedPattern
            : LightPattern.Solid;
        var brightness = Math.Clamp(BackgroundBrightnessSlider.Value / 255d, 0d, 1d);
        var primary = LedPreviewService.ParseColor(BackgroundPrimaryColorBox.Text, "#14B8A6");
        var secondary = LedPreviewService.ParseColor(BackgroundSecondaryColorBox.Text, "#B56CFF");
        var tertiary = LedPreviewService.ParseColor(BackgroundTertiaryColorBox.Text, "#FFFFFF");
        var count = _backgroundLedPreviewDots.Count;
        _backgroundLedPreviewStep++;
        var frame = LedPreviewService.BuildFrame(pattern, _backgroundLedPreviewStep, count, brightness, primary, secondary, tertiary, _previewRandom);

        for (var i = 0; i < count; i++)
        {
            _backgroundLedPreviewDots[i] = PreviewDot(frame[i], brightness);
        }
    }

    private void SetBackgroundLedPreviewAll(string color)
    {
        ResizeLedPreviewDots(_backgroundLedPreviewDots, BackgroundLedPreviewPanel.ActualWidth);
        var previewColor = LedPreviewService.ParseColor(color, "#334155");
        for (var i = 0; i < _backgroundLedPreviewDots.Count; i++)
        {
            _backgroundLedPreviewDots[i] = PreviewDot(previewColor, 0.08);
        }
    }

    private void UpdateBackgroundLedPreviewTimerState()
    {
        if (_initializingComponent || !Dispatcher.CheckAccess())
        {
            return;
        }

        var shouldRun = ShouldRunBackgroundLedPreview();
        if (shouldRun)
        {
            if (!_backgroundLedPreviewTimer.IsEnabled)
            {
                _backgroundLedPreviewTimer.Start();
            }

            return;
        }

        if (_backgroundLedPreviewTimer.IsEnabled)
        {
            _backgroundLedPreviewTimer.Stop();
        }

        if (BackgroundEnabledCheck.IsChecked != true || !_config.ArduinoEnabled)
        {
            SetBackgroundLedPreviewAll("#334155");
        }
    }

    private bool ShouldRunBackgroundLedPreview()
    {
        return BackgroundEnabledCheck.IsChecked == true
            && _config.ArduinoEnabled
            && MainTabs.SelectedIndex == 3
            && BackgroundLedPreviewPanel.IsVisible;
    }
}
