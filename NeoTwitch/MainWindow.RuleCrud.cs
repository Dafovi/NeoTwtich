using System.Windows;
using NeoTwitch.Models;
using NeoTwitch.Services;
using WpfMessageBox = System.Windows.MessageBox;

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

        var copy = rule.Duplicate();
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
        if (WpfMessageBox.Show(this, $"Eliminar la alerta '{rule.Name}'?", "Alertas", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
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
