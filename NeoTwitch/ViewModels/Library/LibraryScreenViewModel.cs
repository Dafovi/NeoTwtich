using System.Collections.ObjectModel;
using NeoTwitch.Services.Library;
using NeoTwitch.ViewModels.Core;

namespace NeoTwitch.ViewModels.Library;

public sealed class LibraryScreenViewModel<TAssetRow, TGroupRow> : ObservableObject
{
    public const string AllFilter = "ALL";

    private string _assetCountText = "0";
    private string _groupCountText = "0";
    private string _lastAssetText = "Sin uso";
    private string _footerText = "";
    private string _searchText = "";
    private string _filter = AllFilter;
    private bool _suppressFilterEvents;

    public LibraryScreenViewModel()
    {
        SelectFilterCommand = new RelayCommand(parameter => SetFilters(SearchText, parameter?.ToString() ?? AllFilter));
    }

    public event EventHandler? FiltersChanged;

    public ObservableCollection<TAssetRow> AssetRows { get; } = [];

    public ObservableCollection<TGroupRow> GroupRows { get; } = [];

    public RelayCommand SelectFilterCommand { get; }

    public string SearchText
    {
        get => _searchText;
        set => SetFilters(value ?? "", Filter);
    }

    public string Filter
    {
        get => _filter;
        private set => SetProperty(ref _filter, NormalizeFilter(value));
    }

    public string AssetCountText
    {
        get => _assetCountText;
        private set => SetProperty(ref _assetCountText, value);
    }

    public string GroupCountText
    {
        get => _groupCountText;
        private set => SetProperty(ref _groupCountText, value);
    }

    public string LastAssetText
    {
        get => _lastAssetText;
        private set => SetProperty(ref _lastAssetText, value);
    }

    public string FooterText
    {
        get => _footerText;
        private set => SetProperty(ref _footerText, value);
    }

    public void SetFilters(string searchText, string filter, bool notify = true)
    {
        _suppressFilterEvents = !notify;
        try
        {
            var changed = SetProperty(ref _searchText, searchText ?? "", nameof(SearchText));
            changed |= SetProperty(ref _filter, NormalizeFilter(filter), nameof(Filter));

            if (changed && notify)
            {
                NotifyFiltersChanged();
            }
        }
        finally
        {
            _suppressFilterEvents = false;
        }
    }

    public void ReplaceAssetRows(IEnumerable<TAssetRow> rows)
    {
        AssetRows.Clear();
        foreach (var row in rows)
        {
            AssetRows.Add(row);
        }
    }

    public void ReplaceGroupRows(IEnumerable<TGroupRow> rows)
    {
        GroupRows.Clear();
        foreach (var row in rows)
        {
            GroupRows.Add(row);
        }
    }

    public void UpdateSummary(LibrarySummaryDisplay summary)
    {
        AssetCountText = summary.AssetCountText;
        GroupCountText = summary.GroupCountText;
        LastAssetText = summary.LastAssetText;
        FooterText = summary.FooterText;
    }

    private void NotifyFiltersChanged()
    {
        if (!_suppressFilterEvents)
        {
            FiltersChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private static string NormalizeFilter(string? filter)
    {
        return string.IsNullOrWhiteSpace(filter) ? AllFilter : filter.Trim().ToUpperInvariant();
    }
}
