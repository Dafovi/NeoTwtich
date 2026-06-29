using NeoTwitch.Models;

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

    private void RefreshRulesView()
    {
        UpdateRuleExternalActionAvailability();
        var selected = RulesList.SelectedItem as EventRule;
        _alertsViewModel.RefreshRules();

        if (selected is not null && _alertsViewModel.ContainsRule(selected))
        {
            RulesList.SelectedItem = selected;
        }
        else if (RulesList.SelectedItem is not EventRule)
        {
            RulesList.SelectedItem = _alertsViewModel.FirstVisibleRule();
        }
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

    private void ShowAllRuleFilters()
    {
        _alertsViewModel.ClearFilters();
    }
}
