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
    private string _newAssetPath = "";
    private string _newAssetName = "";
    private string _newAssetAlertId = "";
    private string _newAssetGroupId = "";
    private string _newGroupName = "";
    private double _volumePercent = 100;
    private bool _suppressFilterEvents;
    private Action<int> _volumeChanged = NoOpInt;

    public LibraryScreenViewModel()
    {
        SelectFilterCommand = new RelayCommand(parameter => SetFilters(SearchText, parameter?.ToString() ?? AllFilter));
        ConfigureActions(NoOp, NoOp, NoOp, NoOp, NoOp, NoOp, NoOp);
    }

    public event EventHandler? FiltersChanged;

    public ObservableCollection<TAssetRow> AssetRows { get; } = [];

    public ObservableCollection<TGroupRow> GroupRows { get; } = [];

    public RelayCommand SelectFilterCommand { get; }

    public RelayCommand BrowseAssetCommand { get; private set; } = new(NoOp);

    public RelayCommand SaveAssetCommand { get; private set; } = new(NoOp);

    public RelayCommand AddGroupCommand { get; private set; } = new(NoOp);

    public RelayCommand ViewGroupCommand { get; private set; } = new(NoOp);

    public RelayCommand DeleteGroupCommand { get; private set; } = new(NoOp);

    public RelayCommand PreviewAssetCommand { get; private set; } = new(NoOp);

    public RelayCommand DeleteAssetCommand { get; private set; } = new(NoOp);

    public double VolumePercent
    {
        get => _volumePercent;
        set => SetVolume(value);
    }

    public string VolumeText => $"{(int)Math.Round(VolumePercent)}%";

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

    public string NewAssetPath
    {
        get => _newAssetPath;
        set => SetProperty(ref _newAssetPath, value ?? "");
    }

    public string NewAssetName
    {
        get => _newAssetName;
        set => SetProperty(ref _newAssetName, value ?? "");
    }

    public string NewAssetAlertId
    {
        get => _newAssetAlertId;
        set => SetProperty(ref _newAssetAlertId, value ?? "");
    }

    public string NewAssetGroupId
    {
        get => _newAssetGroupId;
        set => SetProperty(ref _newAssetGroupId, value ?? "");
    }

    public string NewGroupName
    {
        get => _newGroupName;
        set => SetProperty(ref _newGroupName, value ?? "");
    }

    public void ConfigureActions(
        Action browseAsset,
        Action saveAsset,
        Action addGroup,
        Action<object?> viewGroup,
        Action<object?> deleteGroup,
        Action<object?> previewAsset,
        Action<object?> deleteAsset)
    {
        BrowseAssetCommand = new RelayCommand(browseAsset);
        SaveAssetCommand = new RelayCommand(saveAsset);
        AddGroupCommand = new RelayCommand(addGroup);
        ViewGroupCommand = new RelayCommand(viewGroup);
        DeleteGroupCommand = new RelayCommand(deleteGroup);
        PreviewAssetCommand = new RelayCommand(previewAsset);
        DeleteAssetCommand = new RelayCommand(deleteAsset);

        OnPropertyChanged(nameof(BrowseAssetCommand));
        OnPropertyChanged(nameof(SaveAssetCommand));
        OnPropertyChanged(nameof(AddGroupCommand));
        OnPropertyChanged(nameof(ViewGroupCommand));
        OnPropertyChanged(nameof(DeleteGroupCommand));
        OnPropertyChanged(nameof(PreviewAssetCommand));
        OnPropertyChanged(nameof(DeleteAssetCommand));
    }

    public void ConfigureVolume(Action<int> volumeChanged)
    {
        _volumeChanged = volumeChanged ?? NoOpInt;
    }

    public void SetVolume(double value, bool notify = true)
    {
        var normalized = Math.Clamp(value, 0, 100);
        if (!SetProperty(ref _volumePercent, normalized, nameof(VolumePercent)))
        {
            return;
        }

        OnPropertyChanged(nameof(VolumeText));

        if (notify)
        {
            _volumeChanged((int)Math.Round(normalized));
        }
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

    public void SetNewAssetPath(string path, string suggestedName)
    {
        NewAssetPath = path;
        if (string.IsNullOrWhiteSpace(NewAssetName))
        {
            NewAssetName = suggestedName;
        }
    }

    public void ClearNewAssetForm()
    {
        NewAssetPath = "";
        NewAssetName = "";
        NewAssetAlertId = "";
        NewAssetGroupId = "";
    }

    public void SelectNewAssetGroup(string groupId)
    {
        NewAssetGroupId = groupId;
        NewGroupName = "";
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

    private static void NoOp()
    {
    }

    private static void NoOp(object? parameter)
    {
    }

    private static void NoOpInt(int value)
    {
    }
}
