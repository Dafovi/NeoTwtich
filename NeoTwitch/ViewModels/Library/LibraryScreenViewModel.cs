using System.Collections.ObjectModel;
using NeoTwitch.Services.Library;
using NeoTwitch.ViewModels.Core;

namespace NeoTwitch.ViewModels.Library;

public sealed class LibraryScreenViewModel<TAssetRow, TGroupRow> : ObservableObject
{
    private string _assetCountText = "0";
    private string _groupCountText = "0";
    private string _lastAssetText = "Sin uso";
    private string _footerText = "";

    public ObservableCollection<TAssetRow> AssetRows { get; } = [];

    public ObservableCollection<TGroupRow> GroupRows { get; } = [];

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
}
