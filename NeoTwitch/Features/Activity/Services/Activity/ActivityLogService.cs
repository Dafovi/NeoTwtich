using System.Collections.ObjectModel;
using NeoTwitch.ViewModels.Activity;

namespace NeoTwitch.Services.Activity;

public sealed class ActivityLogService
{
    public const int MaxActivityEntries = 250;
    public const int MaxDashboardEntries = 10;

    public static readonly IReadOnlyList<string> DefaultFilters =
    [
        "TWITCH",
        "ARDUINO",
        "ALEXA",
        "AUDIO",
        "OBS",
        "EVENTO",
        "SISTEMA",
        "IMPORTANTE"
    ];

    private readonly HashSet<string> _enabledFilters = new(DefaultFilters, StringComparer.OrdinalIgnoreCase);
    private readonly TimeProvider _timeProvider;

    public ActivityLogService()
        : this(TimeProvider.System)
    {
    }

    public ActivityLogService(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }

    public ObservableCollection<ActivityLogEntry> Entries { get; } = [];

    public ObservableCollection<ActivityLogEntry> DashboardEntries { get; } = [];

    public IReadOnlySet<string> EnabledFilters => _enabledFilters;

    public string SearchText { get; private set; } = "";

    public void SetFilter(string filter, bool enabled)
    {
        if (string.IsNullOrWhiteSpace(filter))
        {
            return;
        }

        if (enabled)
        {
            _enabledFilters.Add(filter);
        }
        else
        {
            _enabledFilters.Remove(filter);
        }
    }

    public void ResetFilters()
    {
        _enabledFilters.Clear();
        foreach (var filter in DefaultFilters)
        {
            _enabledFilters.Add(filter);
        }
    }

    public void SetSearchText(string? searchText)
    {
        SearchText = searchText?.Trim() ?? "";
    }

    public bool Matches(ActivityLogEntry entry)
    {
        return entry.MatchesFilter(_enabledFilters, SearchText);
    }

    public ActivityLogEntry Add(string message, ActivityLogKind kind)
    {
        var entry = new ActivityLogEntry(message, kind, _timeProvider.GetLocalNow());
        Entries.Insert(0, entry);
        DashboardEntries.Insert(0, entry);
        Trim(Entries, MaxActivityEntries);
        Trim(DashboardEntries, MaxDashboardEntries);
        return entry;
    }

    public void Clear()
    {
        Entries.Clear();
        DashboardEntries.Clear();
    }

    private static void Trim(ObservableCollection<ActivityLogEntry> entries, int maxCount)
    {
        while (entries.Count > maxCount)
        {
            entries.RemoveAt(entries.Count - 1);
        }
    }
}
