using System.IO;
using System.Windows.Media.Imaging;
using NeoTwitch.Models;

namespace NeoTwitch.Services.Library;

public static class MediaMetadataService
{
    public static string BuildVideoMetadata(MediaAssetConfig asset)
    {
        if (asset.DurationMs > 0)
        {
            return asset.DurationText;
        }

        var extension = Path.GetExtension(asset.FilePath);
        return string.IsNullOrWhiteSpace(extension)
            ? "Video"
            : extension.TrimStart('.').ToUpperInvariant();
    }

    public static (int Width, int Height) ProbeImageSize(string path)
    {
        try
        {
            var frame = BitmapFrame.Create(new Uri(path), BitmapCreateOptions.DelayCreation, BitmapCacheOption.None);
            return (frame.PixelWidth, frame.PixelHeight);
        }
        catch
        {
            return (0, 0);
        }
    }
}
