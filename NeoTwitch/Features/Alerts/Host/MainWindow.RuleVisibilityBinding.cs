using NeoTwitch.Services.Ui;

namespace NeoTwitch;

public partial class MainWindow
{
    private void UpdateRuleOptionVisibility()
    {
        var visibility = OptionVisibilityService.ResolveRule(BuildRuleOptionVisibilityInput());

        ApplyRuleOptionVisibility(visibility);
        UpdateRuleAudioModeSelection();
        UpdateRuleObsMediaModeSelection();
        RefreshRuleObsMediaChoices();
        UpdateRuleLedPreviewFrame();
        UpdateVirtualRuleLedPreviewFrame();
        UpdateRuleLedPreviewTimerState();
    }
}
