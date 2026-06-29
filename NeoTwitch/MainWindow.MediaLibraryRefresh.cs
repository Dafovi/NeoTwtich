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

            var libraryViewModel = GetMediaLibraryViewModel(kind);
            libraryViewModel.ReplaceAssetRows(rows);

            libraryViewModel.ReplaceGroupRows(LibraryGroupRowFactoryService.CreateMediaGroupRows(
                groups,
                library,
                count => _text.Format(UiTextKeys.LibraryFileCount, count, count == 1 ? "" : "s")));

            var summary = LibrarySummaryService.Create(
                library,
                groups,
                rows.Length,
                groupFilterId,
                groupsById,
                _text.Get(MediaLibraryKindCatalog.Get(kind).FooterNounKey),
                GetLibrarySummaryLabels());

            libraryViewModel.UpdateSummary(summary);

            if (kind == MediaLibraryKind.Image)
            {
                NewImageGroupBox.Items.Refresh();
            }
            else
            {
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
