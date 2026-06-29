using System.Windows;
using System.Windows.Controls;
using NeoTwitch.Models;

namespace NeoTwitch;

public partial class MainWindow
{
    internal void RulesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_initializingComponent || _loadingUi || _suppressRuleSelectionChange)
        {
            return;
        }

        if (_hasUnsavedRuleChanges
            && _editingRule is not null
            && _alertsViewModel.SelectedRule is EventRule selected
            && !ReferenceEquals(selected, _editingRule))
        {
            if (!ResolvePendingRuleChanges())
            {
                try
                {
                    _suppressRuleSelectionChange = true;
                    _alertsViewModel.SelectedRule = _editingRule;
                }
                finally
                {
                    _suppressRuleSelectionChange = false;
                }

                return;
            }
        }

        LoadSelectedRuleIntoUi();
    }

    internal void StripsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_initializingComponent || _loadingUi)
        {
            return;
        }

        LoadSelectedStripIntoUi();
    }

    internal void RuleFieldChanged(object sender, RoutedEventArgs e)
    {
        if (_initializingComponent || _loadingRule)
        {
            return;
        }

        if (SaveCurrentRuleFromFields())
        {
            UpdateRuleDirtyStateFromSnapshot();
        }

        UpdateEventKindTileSelection();
        UpdatePatternTileSelection();
        UpdateRuleOptionVisibility();
        UpdateRuleLedPreviewTimerState();
    }
}
