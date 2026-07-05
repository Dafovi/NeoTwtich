using System.Collections.ObjectModel;
using NeoTwitch.Models;
using NeoTwitch.Services.Library;
using NeoTwitch.Services.Text;
using NeoTwitch.ViewModels.Library;

namespace NeoTwitch;

public partial class MainWindow
{
    private ObservableCollection<MediaAssetConfig> GetMediaLibrary(MediaLibraryKind kind)
    {
        return kind == MediaLibraryKind.Image ? _config.ImageLibrary : _config.VideoLibrary;
    }

    private ObservableCollection<MediaGroupConfig> GetMediaGroups(MediaLibraryKind kind)
    {
        return kind == MediaLibraryKind.Image ? _config.ImageGroups : _config.VideoGroups;
    }

    private LibraryScreenViewModel<MediaLibraryRow, MediaGroupRow> GetMediaLibraryViewModel(MediaLibraryKind kind)
    {
        return kind == MediaLibraryKind.Image ? _imageLibraryViewModel : _videoLibraryViewModel;
    }

    private string GetMediaSearchText(MediaLibraryKind kind)
    {
        return GetMediaLibraryViewModel(kind).SearchText.Trim();
    }

    private string GetMediaFilter(MediaLibraryKind kind)
    {
        return GetMediaLibraryViewModel(kind).Filter;
    }

    private string GetMediaGroupFilterId(MediaLibraryKind kind)
    {
        return GetMediaLibraryViewModel(kind).GroupFilterId;
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

    private LibrarySummaryLabels GetLibrarySummaryLabels()
    {
        return new LibrarySummaryLabels(
            _text.Get(UiTextKeys.LibrarySummaryFooter),
            _text.Get(UiTextKeys.LibrarySummaryGroupFilter),
            _text.Get(UiTextKeys.LibraryLastUnused),
            _text.Get(UiTextKeys.LibrarySelectedGroup));
    }

}
