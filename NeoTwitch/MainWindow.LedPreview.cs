using System.Collections.ObjectModel;
using NeoTwitch.Models;
using NeoTwitch.Services.Lights;
using static NeoTwitch.Services.Ui.UiBrushFactory;

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

    private static RuleLedPreviewDot PreviewDot(System.Windows.Media.Color color, double brightness)
    {
        var glowOpacity = Math.Clamp(0.12 + (brightness * 0.72), 0.12, 0.9);
        var glowRadius = 7d + (brightness * 22d);
        return new RuleLedPreviewDot(
            FrozenBrushFrom($"#{color.R:X2}{color.G:X2}{color.B:X2}"),
            color,
            glowOpacity,
            glowRadius);
    }

    private static void ResizeLedPreviewDots(ObservableCollection<RuleLedPreviewDot> dots, double availableWidth)
    {
        var targetCount = LedPreviewService.CalculateDotCount(availableWidth);
        while (dots.Count < targetCount)
        {
            dots.Add(PreviewDot(LedPreviewService.ParseColor("#334155", "#334155"), 0.08));
        }

        while (dots.Count > targetCount)
        {
            dots.RemoveAt(dots.Count - 1);
        }
    }
}
