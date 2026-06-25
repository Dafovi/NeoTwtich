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
                "Imagenes|*.png;*.jpg;*.jpeg;*.gif;*.bmp;*.webp|Todos los archivos|*.*",
                "imagenes",
                ObsMediaKind.Image,
                NeoTwitchProduct.Obs.PreviewImageSourceName),
            MediaLibraryKind.Video => new MediaLibraryKindInfo(
                kind,
                UiTextKeys.VideosTitle,
                "#B56CFF",
                "Assets/Icons/media_video.png",
                "Videos|*.mp4;*.mov;*.webm;*.mkv;*.avi;*.wmv|Todos los archivos|*.*",
                "videos",
                ObsMediaKind.Video,
                NeoTwitchProduct.Obs.PreviewVideoSourceName),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Tipo de biblioteca no soportado.")
        };
    }
}

public sealed record MediaLibraryKindInfo(
    MediaLibraryKind Kind,
    string TitleKey,
    string AccentColor,
    string IconPath,
    string FileDialogFilter,
    string FooterNoun,
    ObsMediaKind ObsKind,
    string PreviewSourceName);
