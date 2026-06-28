using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using NeoTwitch.Models;
using NeoTwitch.Services.Alerts;
using NeoTwitch.Services.Text;
using NeoTwitch.Services.Ui;

namespace NeoTwitch;

public partial class MainWindow
{
    internal void RuleSearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_loadingUi || sender is not System.Windows.Controls.TextBox textBox)
        {
            return;
        }

        _ruleSearchText = textBox.Text.Trim();
        RefreshRulesView();
    }

    internal void RuleStatusFilterButton_Click(object sender, RoutedEventArgs e)
    {
        if (_loadingUi || sender is not ToggleButton button)
        {
            return;
        }

        button.IsChecked = true;
        _ruleStatusFilter = button.Tag?.ToString() ?? EventRuleFilterService.AllStatus;

        foreach (var filterButton in RuleStatusFilterButtons())
        {
            if (!ReferenceEquals(filterButton, button))
            {
                filterButton.IsChecked = false;
            }

            ApplyRuleStatusFilterButtonTheme(filterButton, _config.DarkMode ? ThemePalette.Dark : ThemePalette.Light);
        }

        RefreshRulesView();
    }

    internal void RuleCategoryFilterBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_loadingUi)
        {
            return;
        }

        _ruleCategoryFilter = RuleCategoryFilterBox.SelectedValue?.ToString() ?? "";
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
            _ruleStatusFilter,
            _ruleCategoryFilter,
            _ruleSearchText,
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
        if (_initializingComponent || RulesCountText is null)
        {
            return;
        }

        var visibleCount = _rulesViewSource.View?.Cast<EventRule>().Count() ?? 0;
        RulesCountText.Text = $"Mostrando {visibleCount} de {_config.Rules.Count} alertas";
    }

    private void ShowAllRuleFilters()
    {
        _ruleStatusFilter = EventRuleFilterService.AllStatus;
        _ruleCategoryFilter = "";
        RuleFilterAllButton.IsChecked = true;
        RuleFilterActiveButton.IsChecked = false;
        RuleFilterInactiveButton.IsChecked = false;
        RuleCategoryFilterBox.SelectedValue = "";
        RuleSearchBox.Text = "";
        _ruleSearchText = "";
    }
}
