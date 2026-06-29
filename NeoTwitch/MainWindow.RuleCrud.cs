using System.Windows;
using NeoTwitch.Models;
using NeoTwitch.Services;
using WpfMessageBox = System.Windows.MessageBox;

namespace NeoTwitch;

public partial class MainWindow
{
    internal void AddRuleButton_Click(object sender, RoutedEventArgs e)
    {
        if (!ResolvePendingRuleChanges())
        {
            return;
        }

        var rule = ConfigurationItemFactory.CreateRule(_text);
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
                RulesList.SelectedItem = _alertsViewModel.FirstVisibleRule();
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
