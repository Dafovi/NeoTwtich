using NeoTwitch.Models;
using NeoTwitch.Services.Lights;
using NeoTwitch.ViewModels.Shell;

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
        var editor = _alertsViewModel.Editor;
        var pattern = editor.Pattern;
        var brightness = Math.Clamp(editor.Brightness / 255d, 0d, 1d);
        var primary = LedPreviewService.ParseColor(editor.PrimaryColor, "#14B8A6");
        var secondary = LedPreviewService.ParseColor(editor.SecondaryColor, "#B56CFF");
        var tertiary = LedPreviewService.ParseColor(editor.TertiaryColor, "#FFFFFF");
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

        if (!_alertsViewModel.Editor.UseLights || !_config.ArduinoEnabled)
        {
            SetRuleLedPreviewAll("#334155");
        }
    }

    private bool ShouldRunRuleLedPreview()
    {
        return _alertsViewModel.Editor.UseLights
            && _config.ArduinoEnabled
            && _shellViewModel.SelectedTabIndex == ShellViewModel.AlertsTabIndex
            && LightConfigurationPanel.IsVisible
            && RuleLedPreviewPanel.IsVisible;
    }
}
