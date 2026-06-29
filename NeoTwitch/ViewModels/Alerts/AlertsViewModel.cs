using System.ComponentModel;
using System.Windows.Input;
using System.Windows.Data;
using NeoTwitch.Models;
using NeoTwitch.Services.Alerts;
using NeoTwitch.Services.Text;
using NeoTwitch.ViewModels.Core;
using NeoTwitch.ViewModels.Ui;

namespace NeoTwitch.ViewModels.Alerts;

public sealed class AlertsViewModel : ObservableObject
{
    private string _searchText = "";
    private string _statusFilter = EventRuleFilterService.AllStatus;
    private string _categoryFilter = "";
    private string _rulesCountText = "";
    private bool _isEditorEnabled;
    private bool _hasUnsavedChanges;
    private bool _suppressFilterEvents;
    private EventRule? _selectedRule;
    private readonly CollectionViewSource _rulesViewSource = new();
    private readonly IUiTextService _text;
    private IList<EventRule>? _rules;

    public AlertsViewModel(IReadOnlyList<UiOption<string>> categoryOptions)
        : this(categoryOptions, UiTextService.CreateDefault())
    {
    }

    public AlertsViewModel(IReadOnlyList<UiOption<string>> categoryOptions, IUiTextService text)
    {
        CategoryOptions = categoryOptions;
        _text = text;
        _rulesViewSource.Filter += RulesViewSource_Filter;
        SelectStatusFilterCommand = new RelayCommand(parameter => SelectStatusFilter(parameter?.ToString()));
        ConfigureActions(NoOp, NoOp, NoOp, NoOp, NoOp);
    }

    public event EventHandler? FiltersChanged;

    public ICollectionView RulesView => _rulesViewSource.View;

    public IReadOnlyList<UiOption<string>> CategoryOptions { get; }

    public ICommand SelectStatusFilterCommand { get; }

    public ICommand AddRuleCommand { get; private set; } = new RelayCommand(NoOp);

    public ICommand DuplicateRuleCommand { get; private set; } = new RelayCommand(NoOp);

    public ICommand TestRuleCommand { get; private set; } = new RelayCommand(NoOp);

    public ICommand SaveRuleCommand { get; private set; } = new RelayCommand(NoOp);

    public ICommand RemoveRuleCommand { get; private set; } = new RelayCommand(NoOp);

    public EventRule? SelectedRule
    {
        get => _selectedRule;
        set => SetProperty(ref _selectedRule, value);
    }

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

    public bool IsEditorEnabled
    {
        get => _isEditorEnabled;
        private set => SetProperty(ref _isEditorEnabled, value);
    }

    public bool HasUnsavedChanges
    {
        get => _hasUnsavedChanges;
        private set
        {
            if (SetProperty(ref _hasUnsavedChanges, value))
            {
                OnPropertyChanged(nameof(SaveButtonOpacity));
                OnPropertyChanged(nameof(SaveButtonToolTip));
            }
        }
    }

    public double SaveButtonOpacity => HasUnsavedChanges ? 1d : 0.68d;

    public string SaveButtonToolTip => HasUnsavedChanges
        ? "Hay cambios pendientes por guardar"
        : "No hay cambios pendientes";

    public bool IsAllStatusSelected => StatusFilter == EventRuleFilterService.AllStatus;

    public bool IsActiveStatusSelected => StatusFilter == EventRuleFilterService.ActiveStatus;

    public bool IsInactiveStatusSelected => StatusFilter == EventRuleFilterService.InactiveStatus;

    public void ConfigureActions(
        Action addRule,
        Action duplicateRule,
        Action testRule,
        Action saveRule,
        Action removeRule)
    {
        AddRuleCommand = new RelayCommand(addRule);
        DuplicateRuleCommand = new RelayCommand(duplicateRule);
        TestRuleCommand = new RelayCommand(testRule);
        SaveRuleCommand = new RelayCommand(saveRule);
        RemoveRuleCommand = new RelayCommand(removeRule);

        OnPropertyChanged(nameof(AddRuleCommand));
        OnPropertyChanged(nameof(DuplicateRuleCommand));
        OnPropertyChanged(nameof(TestRuleCommand));
        OnPropertyChanged(nameof(SaveRuleCommand));
        OnPropertyChanged(nameof(RemoveRuleCommand));
    }

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

    public void SetRulesSource(IList<EventRule> rules)
    {
        _rules = rules;
        _rulesViewSource.Source = rules;
        RefreshRules();
    }

    public void RefreshRules()
    {
        _rulesViewSource.View?.Refresh();
        UpdateRulesCount(_rulesViewSource.View?.Cast<EventRule>().Count() ?? 0, _rules?.Count ?? 0);
    }

    public bool ContainsRule(EventRule rule)
    {
        return _rulesViewSource.View?.Contains(rule) == true;
    }

    public EventRule? FirstVisibleRule()
    {
        return _rulesViewSource.View?.Cast<EventRule>().FirstOrDefault();
    }

    public void SetEditorEnabled(bool isEnabled)
    {
        IsEditorEnabled = isEnabled;
    }

    public void SetDirtyState(bool isDirty)
    {
        HasUnsavedChanges = isDirty;
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
        if (e.Item is not EventRule rule)
        {
            e.Accepted = false;
            return;
        }

        e.Accepted = EventRuleFilterService.Matches(
            rule,
            StatusFilter,
            CategoryFilter,
            SearchText,
            _text);
    }

    private static void NoOp()
    {
    }
}
