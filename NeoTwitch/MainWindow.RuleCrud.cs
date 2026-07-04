using NeoTwitch.Models;
using NeoTwitch.Services;
using NeoTwitch.Services.Alerts;

namespace NeoTwitch;

public partial class MainWindow
{
    private void AddRule()
    {
        if (!ResolvePendingRuleChanges())
        {
            return;
        }

        var rule = ConfigurationItemFactory.CreateRule(_text);
        _config.Rules.Add(rule);
        ShowAllRuleFilters();
        RefreshRulesView();
        _alertsViewModel.SelectedRule = rule;
        SaveConfig();
        ScheduleTwitchSubscriptionRefreshIfNeeded();
    }

    private void DuplicateSelectedRule()
    {
        if (!ResolvePendingRuleChanges())
        {
            return;
        }

        if (_alertsViewModel.SelectedRule is not EventRule rule)
        {
            return;
        }

        var copy = EventRuleSnapshotService.Duplicate(rule, _text);
        _config.Rules.Add(copy);
        ShowAllRuleFilters();
        RefreshRulesView();
        _alertsViewModel.SelectedRule = copy;
        SaveConfig();
        ScheduleTwitchSubscriptionRefreshIfNeeded();
    }

    private void RemoveSelectedRule()
    {
        if (!ResolvePendingRuleChanges())
        {
            return;
        }

        if (_alertsViewModel.SelectedRule is not EventRule rule)
        {
            return;
        }

        RemoveRule(rule);
    }

    private void RemoveRule(EventRule rule)
    {
        if (!_dialog.Confirm(
            _text.Get(Services.Text.UiTextKeys.AlertsTitle),
            _text.Format(Services.Text.UiTextKeys.RuleDeletePrompt, rule.Name)))
        {
            return;
        }

        var wasSelected = ReferenceEquals(_alertsViewModel.SelectedRule, rule);
        _config.Rules.Remove(rule);
        RefreshRulesView();

        if (_config.Rules.Count > 0)
        {
            if (wasSelected || _alertsViewModel.SelectedRule is null)
            {
                _alertsViewModel.SelectedRule = _alertsViewModel.FirstVisibleRule();
            }
        }
        else
        {
            LoadSelectedRuleIntoUi();
        }

        SaveConfig();
        ScheduleTwitchSubscriptionRefreshIfNeeded();
    }

}
