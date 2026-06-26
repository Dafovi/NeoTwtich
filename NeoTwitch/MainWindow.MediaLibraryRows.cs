using NeoTwitch.Models;
using NeoTwitch.Services.Library;
using NeoTwitch.Services.Text;
using NeoTwitch.Services.Ui;
using NeoTwitch.ViewModels.Library;

namespace NeoTwitch;

public partial class MainWindow
{
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
