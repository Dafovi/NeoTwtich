using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Input;
using NeoTwitch.Services.Activity;
using NeoTwitch.ViewModels.Core;

namespace NeoTwitch.ViewModels.Activity;

public sealed class ActivityViewModel : ObservableObject
{
    private readonly ActivityLogService _activityLog;
    private readonly CollectionViewSource _entriesViewSource = new();
    private string _searchText = "";

    public ActivityViewModel(ActivityLogService activityLog)
    {
        _activityLog = activityLog;
        _entriesViewSource.Source = activityLog.Entries;
        _entriesViewSource.Filter += EntriesViewSource_Filter;
        ClearFiltersCommand = new RelayCommand(ClearFilters);
        ClearHistoryCommand = new RelayCommand(ClearHistory);
    }

    public ICollectionView EntriesView => _entriesViewSource.View;

    public ObservableCollection<ActivityLogEntry> DashboardEntries => _activityLog.DashboardEntries;

    public ICommand ClearFiltersCommand { get; }

    public ICommand ClearHistoryCommand { get; }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (!SetProperty(ref _searchText, value))
            {
                return;
            }

            _activityLog.SetSearchText(value);
            Refresh();
        }
    }

    public void SetFilter(string filter, bool enabled)
    {
        _activityLog.SetFilter(filter, enabled);
        Refresh();
    }

    public void ClearFilters()
    {
        _activityLog.ResetFilters();
        SearchText = "";
        Refresh();
    }

    public void ClearHistory()
    {
        _activityLog.Clear();
    }

    public bool IsFilterEnabled(string filter)
    {
        return _activityLog.EnabledFilters.Contains(filter);
    }

    public void Refresh()
    {
        _entriesViewSource.View?.Refresh();
    }

    private void EntriesViewSource_Filter(object sender, FilterEventArgs e)
    {
        if (e.Item is not ActivityLogEntry entry)
        {
            e.Accepted = false;
            return;
        }

        e.Accepted = _activityLog.Matches(entry);
    }
}
