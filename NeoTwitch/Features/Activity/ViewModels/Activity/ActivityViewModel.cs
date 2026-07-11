using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Collections.Specialized;
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
    private bool _twitchFilterEnabled = true;
    private bool _arduinoFilterEnabled = true;
    private bool _alexaFilterEnabled = true;
    private bool _audioFilterEnabled = true;
    private bool _obsFilterEnabled = true;
    private bool _eventFilterEnabled = true;
    private bool _systemFilterEnabled = true;
    private bool _importantFilterEnabled = true;
    private bool _suppressFilterUpdates;
    private string _latestConsoleText = "Sin actividad reciente.";

    public ActivityViewModel(ActivityLogService activityLog)
    {
        _activityLog = activityLog;
        _entriesViewSource.Source = activityLog.Entries;
        _entriesViewSource.Filter += EntriesViewSource_Filter;
        _activityLog.Entries.CollectionChanged += Entries_CollectionChanged;
        ClearFiltersCommand = new RelayCommand(ClearFilters);
        ClearHistoryCommand = new RelayCommand(ClearHistory);
    }

    public ICollectionView EntriesView => _entriesViewSource.View;

    public ObservableCollection<ActivityLogEntry> DashboardEntries => _activityLog.DashboardEntries;

    public ICommand ClearFiltersCommand { get; }

    public ICommand ClearHistoryCommand { get; }

    public string LatestConsoleText
    {
        get => _latestConsoleText;
        private set => SetProperty(ref _latestConsoleText, value);
    }

    public bool TwitchFilterEnabled
    {
        get => _twitchFilterEnabled;
        set => SetFilterProperty(ref _twitchFilterEnabled, value, "TWITCH");
    }

    public bool ArduinoFilterEnabled
    {
        get => _arduinoFilterEnabled;
        set => SetFilterProperty(ref _arduinoFilterEnabled, value, "ARDUINO");
    }

    public bool AlexaFilterEnabled
    {
        get => _alexaFilterEnabled;
        set => SetFilterProperty(ref _alexaFilterEnabled, value, "ALEXA");
    }

    public bool AudioFilterEnabled
    {
        get => _audioFilterEnabled;
        set => SetFilterProperty(ref _audioFilterEnabled, value, "AUDIO");
    }

    public bool ObsFilterEnabled
    {
        get => _obsFilterEnabled;
        set => SetFilterProperty(ref _obsFilterEnabled, value, "OBS");
    }

    public bool EventFilterEnabled
    {
        get => _eventFilterEnabled;
        set => SetFilterProperty(ref _eventFilterEnabled, value, "EVENTO");
    }

    public bool SystemFilterEnabled
    {
        get => _systemFilterEnabled;
        set => SetFilterProperty(ref _systemFilterEnabled, value, "SISTEMA");
    }

    public bool ImportantFilterEnabled
    {
        get => _importantFilterEnabled;
        set => SetFilterProperty(ref _importantFilterEnabled, value, "IMPORTANTE");
    }

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
        SyncFilterProperty(filter, enabled);
        Refresh();
    }

    public void ClearFilters()
    {
        _activityLog.ResetFilters();
        SetAllFilterProperties(enabled: true);
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

    private void Entries_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        UpdateLatestConsoleText();
    }

    private void UpdateLatestConsoleText()
    {
        LatestConsoleText = _activityLog.Entries.FirstOrDefault() is { } entry
            ? $"{entry.Time}  {entry.SourceName}: {entry.Message}"
            : "Sin actividad reciente.";
    }

    private void SetFilterProperty(ref bool field, bool value, string filter)
    {
        if (!SetProperty(ref field, value) || _suppressFilterUpdates)
        {
            return;
        }

        _activityLog.SetFilter(filter, value);
        Refresh();
    }

    private void SyncFilterProperty(string filter, bool enabled)
    {
        _suppressFilterUpdates = true;
        try
        {
            switch (filter.ToUpperInvariant())
            {
                case "TWITCH":
                    TwitchFilterEnabled = enabled;
                    break;
                case "ARDUINO":
                    ArduinoFilterEnabled = enabled;
                    break;
                case "ALEXA":
                    AlexaFilterEnabled = enabled;
                    break;
                case "AUDIO":
                    AudioFilterEnabled = enabled;
                    break;
                case "OBS":
                    ObsFilterEnabled = enabled;
                    break;
                case "EVENTO":
                    EventFilterEnabled = enabled;
                    break;
                case "SISTEMA":
                    SystemFilterEnabled = enabled;
                    break;
                case "IMPORTANTE":
                    ImportantFilterEnabled = enabled;
                    break;
            }
        }
        finally
        {
            _suppressFilterUpdates = false;
        }
    }

    private void SetAllFilterProperties(bool enabled)
    {
        _suppressFilterUpdates = true;
        try
        {
            TwitchFilterEnabled = enabled;
            ArduinoFilterEnabled = enabled;
            AlexaFilterEnabled = enabled;
            AudioFilterEnabled = enabled;
            ObsFilterEnabled = enabled;
            EventFilterEnabled = enabled;
            SystemFilterEnabled = enabled;
            ImportantFilterEnabled = enabled;
        }
        finally
        {
            _suppressFilterUpdates = false;
        }
    }
}
