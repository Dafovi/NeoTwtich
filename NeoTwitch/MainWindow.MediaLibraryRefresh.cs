using System.Collections.ObjectModel;
using NeoTwitch.Models;
using NeoTwitch.Services.Library;
using NeoTwitch.Services.Text;
using NeoTwitch.Services.Ui;
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

    private void RefreshMediaGroupChoicesIfNeeded(MediaLibraryKind kind)
    {
        var groups = GetMediaGroups(kind);
        var signature = string.Join("|", groups.Select(group => $"{group.Id}:{group.Name}"));
        var currentSignature = kind == MediaLibraryKind.Image
            ? _imageGroupChoicesSignature
            : _videoGroupChoicesSignature;
        if (string.Equals(signature, currentSignature, StringComparison.Ordinal))
        {
            return;
        }

        var choices = kind == MediaLibraryKind.Image ? ImageGroupChoices : VideoGroupChoices;
        choices.Clear();
        choices.Add(new MediaGroupChoice("", _text.Get(UiTextKeys.LibraryNoGroupAssigned)));
        foreach (var group in groups)
        {
            choices.Add(new MediaGroupChoice(group.Id, group.Name));
        }

        if (kind == MediaLibraryKind.Image)
        {
            _imageGroupChoicesSignature = signature;
        }
        else
        {
            _videoGroupChoicesSignature = signature;
        }
    }

    private MediaLibraryRow CreateMediaLibraryRow(
        MediaLibraryKind kind,
        MediaAssetConfig asset,
        IReadOnlyDictionary<string, string> groupsById,
        int index)
    {
        return LibraryRowFactoryService.CreateMediaRow(
            kind,
            asset,
            groupsById,
            _text.Get(UiTextKeys.LibraryNoGroup),
            index,
            _config.Obs.IsConfigured && _obsService.IsConnected && !_isObsConnecting,
            _previewingMediaKind,
            _previewingMediaId);
    }

    private bool MediaRowMatchesFilters(MediaLibraryKind kind, MediaLibraryRow row)
    {
        return LibraryRowFilterService.MatchesMedia(
            row,
            GetMediaGroupFilterId(kind),
            GetMediaFilter(kind),
            GetMediaSearchText(kind));
    }

    private void UpdateMediaFilterButtons(MediaLibraryKind kind)
    {
        if (_initializingComponent)
        {
            return;
        }

        var palette = _config.DarkMode ? ThemePalette.Dark : ThemePalette.Light;
        var accentColor = MediaLibraryKindCatalog.Get(kind).AccentColor;
        var filter = GetMediaFilter(kind);
        var buttons = kind == MediaLibraryKind.Image
            ? new[] { ImageFilterAllButton, ImageFilterWithGroupButton, ImageFilterNoGroupButton }
            : new[] { VideoFilterAllButton, VideoFilterWithGroupButton, VideoFilterNoGroupButton };

        foreach (var button in buttons)
        {
            var active = string.Equals(button.Tag?.ToString(), filter, StringComparison.OrdinalIgnoreCase);
            FilterButtonThemeService.Apply(button, active, accentColor, palette);
        }
    }

}
