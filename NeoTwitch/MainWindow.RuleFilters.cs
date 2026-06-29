using System.Windows.Data;
using NeoTwitch.Models;
using NeoTwitch.Services.Alerts;

namespace NeoTwitch;

public partial class MainWindow
{
    private void AlertsFiltersChanged(object? sender, EventArgs e)
    {
        if (_loadingUi)
        {
            return;
        }

        RefreshRulesView();
    }

    private void RulesViewSource_Filter(object sender, FilterEventArgs e)
    {
        if (e.Item is not EventRule rule)
        {
            e.Accepted = false;
            return;
        }

        e.Accepted = EventRuleFilterService.Matches(
            rule,
            _alertsViewModel.StatusFilter,
            _alertsViewModel.CategoryFilter,
            _alertsViewModel.SearchText,
            _text);
    }

    private void RefreshRulesView()
    {
        UpdateRuleExternalActionAvailability();
        var selected = RulesList.SelectedItem as EventRule;
        _rulesViewSource.View?.Refresh();

        if (selected is not null && _rulesViewSource.View?.Contains(selected) == true)
        {
            RulesList.SelectedItem = selected;
        }
        else if (RulesList.SelectedItem is not EventRule)
        {
            RulesList.SelectedItem = _rulesViewSource.View?.Cast<EventRule>().FirstOrDefault();
        }

        UpdateRulesCountText();
    }

    private void UpdateRuleExternalActionAvailability()
    {
        if (_config.Rules.Count == 0)
        {
            return;
        }

        var lightsAvailable = _config.ArduinoEnabled;
        var alexaAvailable = _config.Alexa.IsConfigured;
        var obsAvailable = _config.Obs.IsConfigured;

        foreach (var rule in _config.Rules)
        {
            rule.LightsActionAvailable = lightsAvailable;
            rule.AlexaActionAvailable = alexaAvailable;
            rule.ObsActionAvailable = obsAvailable;
        }
    }

    private void UpdateRulesCountText()
    {
        if (_initializingComponent)
        {
            return;
        }

        var visibleCount = _rulesViewSource.View?.Cast<EventRule>().Count() ?? 0;
        _alertsViewModel.UpdateRulesCount(visibleCount, _config.Rules.Count);
    }

    private void ShowAllRuleFilters()
    {
        _alertsViewModel.ClearFilters();
    }
}
