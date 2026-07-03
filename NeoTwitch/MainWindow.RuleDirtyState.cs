using NeoTwitch.Models;
using NeoTwitch.Services.Alerts;
using NeoTwitch.Services.Ui;
using static NeoTwitch.Services.Text.UiTextFormatter;

namespace NeoTwitch;

public partial class MainWindow
{
    private void SavePendingRuleChanges()
    {
        if (!SaveCurrentRuleFromFields())
        {
            return;
        }

        SaveConfig();
        CaptureCurrentRuleSnapshot();
        SetRuleDirtyState(false);
        ScheduleTwitchSubscriptionRefreshIfNeeded();
        AddLog("Alerta guardada.");
    }

    private bool ResolvePendingRuleChanges()
    {
        if (!_hasUnsavedRuleChanges)
        {
            return true;
        }

        var ruleName = FirstNonEmpty(_editingRule?.Name ?? "", "esta alerta");
        var result = _dialog.ConfirmWithCancel(
            "Cambios sin guardar",
            $"Hay cambios sin guardar en '{ruleName}'.\n\nQuieres guardarlos antes de continuar?");

        if (result == DialogChoice.Cancel)
        {
            return false;
        }

        if (result == DialogChoice.Yes)
        {
            SavePendingRuleChanges();
            return true;
        }

        DiscardPendingRuleChanges();
        return true;
    }

    private void DiscardPendingRuleChanges()
    {
        var revertedRule = _editingRule;
        if (revertedRule is not null && _loadedRuleSnapshot is not null)
        {
            EventRuleSnapshotService.CopyValues(_loadedRuleSnapshot, revertedRule);
            RefreshRulesView();
            SaveConfig();
            if (ReferenceEquals(_alertsViewModel.SelectedRule, revertedRule))
            {
                LoadSelectedRuleIntoUi();
                return;
            }
        }

        SetRuleDirtyState(false);
    }

    private void CaptureCurrentRuleSnapshot()
    {
        _loadedRuleSnapshot = _editingRule is null
            ? null
            : EventRuleSnapshotService.Clone(_editingRule);
    }

    private void SetRuleDirtyState(bool isDirty)
    {
        _hasUnsavedRuleChanges = isDirty;
        _alertsViewModel.SetDirtyState(isDirty);
    }

    private void UpdateRuleDirtyStateFromSnapshot()
    {
        if (_editingRule is null || _loadedRuleSnapshot is null)
        {
            SetRuleDirtyState(false);
            return;
        }

        SetRuleDirtyState(!EventRuleSnapshotService.HaveSameEditableValues(_loadedRuleSnapshot, _editingRule));
    }
}
