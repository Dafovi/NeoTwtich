using System.Collections.Specialized;
using System.Windows.Data;
using NeoTwitch.Models;
using NeoTwitch.Services.Alerts;

namespace NeoTwitch.ViewModels.Alerts;

public sealed partial class AlertsViewModel
{
    public void SetRulesSource(IList<EventRule> rules)
    {
        if (_rulesCollection is not null)
        {
            _rulesCollection.CollectionChanged -= RulesCollection_CollectionChanged;
        }

        _rules = rules;
        _rulesCollection = rules as INotifyCollectionChanged;
        if (_rulesCollection is not null)
        {
            _rulesCollection.CollectionChanged += RulesCollection_CollectionChanged;
        }

        RebuildRuleRows();
        _rulesViewSource.Source = _ruleRows;
        RefreshRules();
    }

    public void RefreshRules()
    {
        _rulesViewSource.View?.Refresh();
        UpdateRulesCount(_rulesViewSource.View?.Cast<EventRuleRowViewModel>().Count() ?? 0, _rules?.Count ?? 0);
    }

    public bool ContainsRule(EventRule rule)
    {
        var row = FindRow(rule);
        return row is not null && _rulesViewSource.View?.Contains(row) == true;
    }

    public EventRule? FirstVisibleRule()
    {
        return _rulesViewSource.View?.Cast<EventRuleRowViewModel>().FirstOrDefault()?.Rule;
    }

    private void NotifyFiltersChanged()
    {
        if (!_suppressFilterEvents)
        {
            FiltersChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private static string NormalizeStatusFilter(string? status)
    {
        return status?.ToUpperInvariant() switch
        {
            EventRuleFilterService.ActiveStatus => EventRuleFilterService.ActiveStatus,
            EventRuleFilterService.InactiveStatus => EventRuleFilterService.InactiveStatus,
            _ => EventRuleFilterService.AllStatus
        };
    }

    private void RulesViewSource_Filter(object sender, FilterEventArgs e)
    {
        if (e.Item is not EventRuleRowViewModel row)
        {
            e.Accepted = false;
            return;
        }

        e.Accepted = EventRuleFilterService.Matches(
            row.Rule,
            StatusFilter,
            CategoryFilter,
            SearchText,
            _text);
    }

    private void RulesCollection_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        var selectedRule = SelectedRule;
        RebuildRuleRows();

        if (selectedRule is not null && _rules?.Contains(selectedRule) == true)
        {
            SelectedRule = selectedRule;
        }
        else
        {
            SelectedRule = null;
        }

        RefreshRules();
    }

    private void RebuildRuleRows()
    {
        foreach (var row in _ruleRows)
        {
            row.Dispose();
        }

        _ruleRows.Clear();
        if (_rules is not null)
        {
            foreach (var rule in _rules)
            {
                _ruleRows.Add(new EventRuleRowViewModel(rule, _text));
            }
        }

        SetSelectedRuleRow(FindRow(SelectedRule), notifyRuleSelection: false);
        OnPropertyChanged(nameof(SelectedRuleRow));
    }

    private EventRuleRowViewModel? FindRow(EventRule? rule)
    {
        return rule is null
            ? null
            : _ruleRows.FirstOrDefault(row => ReferenceEquals(row.Rule, rule));
    }

    private bool SetSelectedRuleRow(EventRuleRowViewModel? row, bool notifyRuleSelection)
    {
        if (ReferenceEquals(_selectedRuleRow, row))
        {
            return false;
        }

        _selectedRuleRow = row;
        if (notifyRuleSelection)
        {
            SelectedRule = row?.Rule;
        }

        return true;
    }
}
