using System.Collections.ObjectModel;
using NeoTwitch.Models;
using NeoTwitch.Services.Library;
using NeoTwitch.Services.Text;
using NeoTwitch.Services.Ui;
using NeoTwitch.ViewModels.Library;
using static NeoTwitch.Services.Ui.UiBrushFactory;

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
            var groupIndex = 0;
            foreach (var group in groups)
            {
                var count = library.Count(asset => string.Equals(asset.GroupId, group.Id, StringComparison.OrdinalIgnoreCase));
                groupRows.Add(new MediaGroupRow(
                    group.Id,
                    group.Name,
                    _text.Format(UiTextKeys.LibraryFileCount, count, count == 1 ? "" : "s"),
                    FrozenBrushFrom((groupIndex++ % 4) switch
                    {
                        0 => "#14B8A6",
                        1 => "#B56CFF",
                        2 => "#37C7F3",
                        _ => "#22C55E"
                    })));
            }

            var lastAsset = library
                .Where(asset => asset.LastUsedAt is not null)
                .OrderByDescending(asset => asset.LastUsedAt)
                .FirstOrDefault();

            var groupFilterText = string.IsNullOrWhiteSpace(groupFilterId)
                ? ""
                : $" del grupo {groupsById.GetValueOrDefault(groupFilterId, _text.Get(UiTextKeys.LibrarySelectedGroup))}";

            if (kind == MediaLibraryKind.Image)
            {
                ImageSavedCountText.Text = library.Count.ToString();
                ImageGroupCountText.Text = groups.Count.ToString();
                LastImageText.Text = lastAsset?.DisplayName ?? _text.Get(UiTextKeys.LibraryLastUnused);
                ImageLibraryFooterText.Text = $"Mostrando {rows.Length} de {library.Count} {MediaLibraryKindCatalog.Get(kind).FooterNoun}{groupFilterText}";
                NewImageGroupBox.Items.Refresh();
            }
            else
            {
                VideoSavedCountText.Text = library.Count.ToString();
                VideoGroupCountText.Text = groups.Count.ToString();
                LastVideoText.Text = lastAsset?.DisplayName ?? _text.Get(UiTextKeys.LibraryLastUnused);
                VideoLibraryFooterText.Text = $"Mostrando {rows.Length} de {library.Count} {MediaLibraryKindCatalog.Get(kind).FooterNoun}{groupFilterText}";
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
            button.Background = active ? TranslucentBrushFrom(accentColor) : palette.Input;
            button.Foreground = active ? FrozenBrushFrom(accentColor) : palette.Text;
            button.BorderBrush = active ? FrozenBrushFrom(accentColor) : palette.Border;
        }
    }

    private ObservableCollection<MediaAssetConfig> GetMediaLibrary(MediaLibraryKind kind)
    {
        return kind == MediaLibraryKind.Image ? _config.ImageLibrary : _config.VideoLibrary;
    }

    private ObservableCollection<MediaGroupConfig> GetMediaGroups(MediaLibraryKind kind)
    {
        return kind == MediaLibraryKind.Image ? _config.ImageGroups : _config.VideoGroups;
    }

    private ObservableCollection<MediaLibraryRow> GetMediaRows(MediaLibraryKind kind)
    {
        return kind == MediaLibraryKind.Image ? _imageLibraryRows : _videoLibraryRows;
    }

    private ObservableCollection<MediaGroupRow> GetMediaGroupRows(MediaLibraryKind kind)
    {
        return kind == MediaLibraryKind.Image ? _imageGroupRows : _videoGroupRows;
    }

    private string GetMediaSearchText(MediaLibraryKind kind)
    {
        return kind == MediaLibraryKind.Image ? _imageSearchText : _videoSearchText;
    }

    private string GetMediaFilter(MediaLibraryKind kind)
    {
        return kind == MediaLibraryKind.Image ? _imageFilter : _videoFilter;
    }

    private string GetMediaGroupFilterId(MediaLibraryKind kind)
    {
        return kind == MediaLibraryKind.Image ? _imageGroupFilterId : _videoGroupFilterId;
    }

    private void SetMediaRefreshing(MediaLibraryKind kind, bool refreshing)
    {
        if (kind == MediaLibraryKind.Image)
        {
            _refreshingImageLibrary = refreshing;
        }
        else
        {
            _refreshingVideoLibrary = refreshing;
        }
    }

    private string MediaLibraryTitle(MediaLibraryKind kind)
    {
        return _text.Get(MediaLibraryKindCatalog.Get(kind).TitleKey);
    }

    private void UpdateVideoVolumeText()
    {
        VideoVolumeValueText.Text = $"{_config.VideoVolumePercent}%";
    }
}
