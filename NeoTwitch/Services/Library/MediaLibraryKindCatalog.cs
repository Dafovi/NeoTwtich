using NeoTwitch.Models;
using NeoTwitch.Services.Text;
using NeoTwitch.Shared;
using NeoTwitch.ViewModels.Library;

namespace NeoTwitch.Services.Library;

public static class MediaLibraryKindCatalog
{
    public static MediaLibraryKindInfo Get(MediaLibraryKind kind)
    {
        return kind switch
        {
            MediaLibraryKind.Image => new MediaLibraryKindInfo(
                kind,
                UiTextKeys.ImagesTitle,
                "#37C7F3",
                "Assets/Icons/media_image.png",
                UiTextKeys.ImagesFileDialogFilter,
                UiTextKeys.ImagesFooterNoun,
                ObsMediaKind.Image,
                NeoTwitchProduct.Obs.PreviewImageSourceName),
            MediaLibraryKind.Video => new MediaLibraryKindInfo(
                kind,
                UiTextKeys.VideosTitle,
                "#B56CFF",
                "Assets/Icons/media_video.png",
                UiTextKeys.VideosFileDialogFilter,
                UiTextKeys.VideosFooterNoun,
                ObsMediaKind.Video,
                NeoTwitchProduct.Obs.PreviewVideoSourceName),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, nameof(kind))
        };
    }
}

public sealed record MediaLibraryKindInfo(
    MediaLibraryKind Kind,
    string TitleKey,
    string AccentColor,
    string IconPath,
    string FileDialogFilterKey,
    string FooterNounKey,
    ObsMediaKind ObsKind,
    string PreviewSourceName);
