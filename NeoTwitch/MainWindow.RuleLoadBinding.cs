using NeoTwitch.Models;
using NeoTwitch.Services.Lights;

namespace NeoTwitch;

public partial class MainWindow
{
    private void LoadSelectedRuleIntoUi()
    {
        _loadingRule = true;

        try
        {
            _alertsViewModel.SetEditorEnabled(_alertsViewModel.SelectedRule is EventRule);

            if (_alertsViewModel.SelectedRule is not EventRule rule)
            {
                _editingRule = null;
                _loadedRuleSnapshot = null;
                _alertsViewModel.Editor.Clear();
                SetRuleDirtyState(false);
                return;
            }

            _editingRule = rule;
            _alertsViewModel.Editor.LoadBasicFields(rule);
            UpdateEventKindTileSelection();
            RefreshRuleObsMediaChoices();
            RefreshRulePinChoices();
            UpdateColorButtons();
            UpdateSliderLabels();
            UpdatePatternTileSelection();
            UpdateRuleObsMediaModeSelection();
            UpdateRuleLedPreviewFrame();
            CaptureCurrentRuleSnapshot();
            SetRuleDirtyState(false);
        }
        finally
        {
            _loadingRule = false;
            UpdateRuleOptionVisibility();
            UpdateRuleLedPreviewTimerState();
        }
    }
}
