using System.Windows.Input;
using NeoTwitch.Services.Alerts;
using NeoTwitch.ViewModels.Core;
using NeoTwitch.ViewModels.Ui;

namespace NeoTwitch.ViewModels.Alerts;

public sealed class AlertsViewModel : ObservableObject
{
    private string _searchText = "";
    private string _statusFilter = EventRuleFilterService.AllStatus;
    private string _categoryFilter = "";
    private string _rulesCountText = "";
    private bool _suppressFilterEvents;

    public AlertsViewModel(IReadOnlyList<UiOption<string>> categoryOptions)
    {
        CategoryOptions = categoryOptions;
        SelectStatusFilterCommand = new RelayCommand(parameter => SelectStatusFilter(parameter?.ToString()));
    }

    public event EventHandler? FiltersChanged;

    public IReadOnlyList<UiOption<string>> CategoryOptions { get; }

    public ICommand SelectStatusFilterCommand { get; }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value ?? ""))
            {
                NotifyFiltersChanged();
            }
        }
    }

    public string StatusFilter
    {
        get => _statusFilter;
        private set
        {
            if (SetProperty(ref _statusFilter, NormalizeStatusFilter(value)))
            {
                OnPropertyChanged(nameof(IsAllStatusSelected));
                OnPropertyChanged(nameof(IsActiveStatusSelected));
                OnPropertyChanged(nameof(IsInactiveStatusSelected));
                NotifyFiltersChanged();
            }
        }
    }

    public string CategoryFilter
    {
        get => _categoryFilter;
        set
        {
            if (SetProperty(ref _categoryFilter, value ?? ""))
            {
                NotifyFiltersChanged();
            }
        }
    }

    public string RulesCountText
    {
        get => _rulesCountText;
        private set => SetProperty(ref _rulesCountText, value);
    }

    public bool IsAllStatusSelected => StatusFilter == EventRuleFilterService.AllStatus;

    public bool IsActiveStatusSelected => StatusFilter == EventRuleFilterService.ActiveStatus;

    public bool IsInactiveStatusSelected => StatusFilter == EventRuleFilterService.InactiveStatus;

    public void SelectStatusFilter(string? status)
    {
        StatusFilter = NormalizeStatusFilter(status);
    }

    public void ClearFilters()
    {
        _suppressFilterEvents = true;
        try
        {
            SearchText = "";
            CategoryFilter = "";
            StatusFilter = EventRuleFilterService.AllStatus;
        }
        finally
        {
            _suppressFilterEvents = false;
        }

        NotifyFiltersChanged();
    }

    public void UpdateRulesCount(int visibleCount, int totalCount)
    {
        RulesCountText = $"Mostrando {Math.Max(0, visibleCount)} de {Math.Max(0, totalCount)} alertas";
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
}
