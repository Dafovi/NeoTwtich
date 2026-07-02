using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using NeoTwitch.Models;

namespace NeoTwitch;

public partial class MainWindow
{
    private void AlertsSelectedRuleChanged(object? sender, EventArgs e)
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

        UpdateRuleFieldBindingSource(sender);

        if (SaveCurrentRuleFromFields())
        {
            UpdateRuleDirtyStateFromSnapshot();
        }

        UpdateEventKindTileSelection();
        UpdatePatternTileSelection();
        UpdateRuleOptionVisibility();
        UpdateRuleLedPreviewTimerState();
    }

    private static void UpdateRuleFieldBindingSource(object sender)
    {
        switch (sender)
        {
            case System.Windows.Controls.TextBox textBox:
                textBox.GetBindingExpression(System.Windows.Controls.TextBox.TextProperty)?.UpdateSource();
                break;
            case ToggleButton toggleButton:
                toggleButton.GetBindingExpression(ToggleButton.IsCheckedProperty)?.UpdateSource();
                break;
            case Selector selector:
                selector.GetBindingExpression(Selector.SelectedValueProperty)?.UpdateSource();
                break;
            case RangeBase range:
                range.GetBindingExpression(RangeBase.ValueProperty)?.UpdateSource();
                break;
        }
    }
}
