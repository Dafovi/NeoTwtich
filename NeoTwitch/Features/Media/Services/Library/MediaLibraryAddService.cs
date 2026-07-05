using System.IO;
using NeoTwitch.Models;
using NeoTwitch.ViewModels.Library;

namespace NeoTwitch.Services.Library;

public sealed record MediaAssetAddRequest(
    string FilePath,
    string Name,
    string GroupId);

public sealed record MediaAssetAddResult(MediaAssetConfig? Asset, bool Created)
{
    public bool Saved => Asset is not null;
}

public static class MediaLibraryAddService
{
    public static MediaAssetAddResult AddOrUpdate(
        AppConfig config,
        MediaLibraryKind kind,
        MediaAssetAddRequest request,
        Func<string, (int Width, int Height)>? imageSizeProbe = null,
        Func<string, int>? videoDurationProbe = null,
        Func<string, bool>? fileExists = null)
    {
        fileExists ??= File.Exists;
        var path = (request.FilePath ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(path) || !fileExists(path))
        {
            return new MediaAssetAddResult(null, false);
        }

        var library = kind == MediaLibraryKind.Image ? config.ImageLibrary : config.VideoLibrary;
        var existing = library.FirstOrDefault(asset => string.Equals(asset.FilePath, path, StringComparison.OrdinalIgnoreCase));
        var created = existing is null;
        var asset = existing ?? new MediaAssetConfig { FilePath = path };
        asset.Name = string.IsNullOrWhiteSpace(request.Name)
            ? Path.GetFileNameWithoutExtension(path)
            : request.Name.Trim();
        asset.GroupId = request.GroupId ?? string.Empty;

        if (kind == MediaLibraryKind.Image)
        {
            var probe = imageSizeProbe ?? MediaMetadataService.ProbeImageSize;
            var size = probe(path);
            asset.Width = size.Width;
            asset.Height = size.Height;
        }
        else
        {
            var probe = videoDurationProbe ?? MediaMetadataService.ProbeVideoDurationMs;
            asset.DurationMs = probe(path);
        }

        if (created)
        {
            library.Add(asset);
        }

        return new MediaAssetAddResult(asset, created);
    }
}
