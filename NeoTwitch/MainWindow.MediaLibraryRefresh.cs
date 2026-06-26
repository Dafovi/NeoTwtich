using NeoTwitch.Services.Library;
using NeoTwitch.Services.Text;
using NeoTwitch.ViewModels.Library;

namespace NeoTwitch;

public partial class MainWindow
{
    private void RefreshMediaLibraryView(MediaLibraryKind kind)
    {
        if (_initializingComponent)
        {
            return;
        }

        if (!Dispatcher.CheckAccess())
        {
            _ = Dispatcher.InvokeAsync(() => RefreshMediaLibraryView(kind));
            return;
        }

        SetMediaRefreshing(kind, true);
        try
        {
            var library = GetMediaLibrary(kind);
            var groups = GetMediaGroups(kind);
            var groupFilterId = GetMediaGroupFilterId(kind);
            var groupsById = groups.ToDictionary(group => group.Id, group => group.Name, StringComparer.OrdinalIgnoreCase);

            RefreshMediaGroupChoicesIfNeeded(kind);

            var rows = library
                .Select((asset, index) => CreateMediaLibraryRow(kind, asset, groupsById, index))
                .Where(row => MediaRowMatchesFilters(kind, row))
                .ToArray();

            var rowTarget = GetMediaRows(kind);
            rowTarget.Clear();
            foreach (var row in rows)
            {
                rowTarget.Add(row);
            }

            var groupRows = GetMediaGroupRows(kind);
            groupRows.Clear();
            foreach (var row in LibraryGroupRowFactoryService.CreateMediaGroupRows(
                         groups,
                         library,
                         count => _text.Format(UiTextKeys.LibraryFileCount, count, count == 1 ? "" : "s")))
            {
                groupRows.Add(row);
            }

            var summary = LibrarySummaryService.Create(
                library,
                groups,
                rows.Length,
                groupFilterId,
                groupsById,
                MediaLibraryKindCatalog.Get(kind).FooterNoun,
                _text.Get(UiTextKeys.LibraryLastUnused),
                _text.Get(UiTextKeys.LibrarySelectedGroup));

            if (kind == MediaLibraryKind.Image)
            {
                ImageSavedCountText.Text = summary.AssetCountText;
                ImageGroupCountText.Text = summary.GroupCountText;
                LastImageText.Text = summary.LastAssetText;
                ImageLibraryFooterText.Text = summary.FooterText;
                NewImageGroupBox.Items.Refresh();
            }
            else
            {
                VideoSavedCountText.Text = summary.AssetCountText;
                VideoGroupCountText.Text = summary.GroupCountText;
                LastVideoText.Text = summary.LastAssetText;
                VideoLibraryFooterText.Text = summary.FooterText;
                NewVideoGroupBox.Items.Refresh();
            }

            UpdateMediaFilterButtons(kind);
            RefreshRuleObsMediaChoices();
        }
        finally
        {
            SetMediaRefreshing(kind, false);
        }
    }
}
