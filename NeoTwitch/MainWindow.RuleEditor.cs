using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using NeoTwitch.Models;
using NeoTwitch.Services;
using NeoTwitch.Services.Alerts;
using NeoTwitch.Services.Lights;
using NeoTwitch.Services.Ui;
using NeoTwitch.ViewModels.Activity;
using WpfMessageBox = System.Windows.MessageBox;
using static NeoTwitch.Services.Text.UiTextFormatter;

namespace NeoTwitch;

public partial class MainWindow
{
    internal void AddRuleButton_Click(object sender, RoutedEventArgs e)
    {
        if (!ResolvePendingRuleChanges())
        {
            return;
        }

        var rule = ConfigurationItemFactory.CreateRule();
        _config.Rules.Add(rule);
        ShowAllRuleFilters();
        RefreshRulesView();
        RulesList.SelectedItem = rule;
        SaveConfig();
        ScheduleTwitchSubscriptionRefreshIfNeeded();
    }

    internal void DuplicateRuleButton_Click(object sender, RoutedEventArgs e)
    {
        if (!ResolvePendingRuleChanges())
        {
            return;
        }

        if (RulesList.SelectedItem is not EventRule rule)
        {
            return;
        }

        var copy = rule.Duplicate();
        _config.Rules.Add(copy);
        ShowAllRuleFilters();
        RefreshRulesView();
        RulesList.SelectedItem = copy;
        SaveConfig();
        ScheduleTwitchSubscriptionRefreshIfNeeded();
    }

    internal void RemoveRuleButton_Click(object sender, RoutedEventArgs e)
    {
        if (!ResolvePendingRuleChanges())
        {
            return;
        }

        if (RulesList.SelectedItem is not EventRule rule)
        {
            return;
        }

        RemoveRule(rule);
    }

    private void RemoveRule(EventRule rule)
    {
        if (WpfMessageBox.Show(this, $"Eliminar la alerta '{rule.Name}'?", "Alertas", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
        {
            return;
        }

        var wasSelected = ReferenceEquals(RulesList.SelectedItem, rule);
        _config.Rules.Remove(rule);
        RefreshRulesView();

        if (_config.Rules.Count > 0)
        {
            if (wasSelected || RulesList.SelectedItem is not EventRule)
            {
                RulesList.SelectedItem = _rulesViewSource.View?.Cast<EventRule>().FirstOrDefault();
            }
        }
        else
        {
            LoadSelectedRuleIntoUi();
        }

        SaveConfig();
        ScheduleTwitchSubscriptionRefreshIfNeeded();
    }

    internal void SaveRuleButton_Click(object sender, RoutedEventArgs e)
    {
        SavePendingRuleChanges();
    }

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
        var result = WpfMessageBox.Show(
            this,
            $"Hay cambios sin guardar en '{ruleName}'.\n\nQuieres guardarlos antes de continuar?",
            "Cambios sin guardar",
            MessageBoxButton.YesNoCancel,
            MessageBoxImage.Warning);

        if (result == MessageBoxResult.Cancel)
        {
            return false;
        }

        if (result == MessageBoxResult.Yes)
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
            if (ReferenceEquals(RulesList.SelectedItem, revertedRule))
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

        if (SaveRuleButton is not null)
        {
            SaveRuleButton.Opacity = isDirty ? 1d : 0.68d;
            SaveRuleButton.ToolTip = isDirty
                ? "Hay cambios pendientes por guardar"
                : "No hay cambios pendientes";
        }
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
