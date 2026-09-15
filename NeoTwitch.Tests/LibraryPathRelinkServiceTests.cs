using NeoTwitch.Models;
using NeoTwitch.Models.Library;
using NeoTwitch.Services.Library;

static class LibraryPathRelinkServiceTests
{
    public static void RelinksUniqueFilenames()
    {
        var audio = new AudioAssetConfig { FilePath = @"D:\Old\sounds\alert.mp3" };
        var image = new MediaAssetConfig { FilePath = @"D:\Old\images\alert.png" };
        var duplicate = new MediaAssetConfig { FilePath = @"D:\Old\images\duplicate.png" };
        var assets = new ILibraryAssetConfig[] { audio, image, duplicate };
        var files = new[]
        {
            @"E:\Imported\sounds\alert.mp3",
            @"E:\Imported\images\alert.png",
            @"E:\Imported\first\duplicate.png",
            @"E:\Imported\second\duplicate.png"
        };

        var result = LibraryPathRelinkService.RelinkMissingAssets(
            assets,
            @"E:\Imported",
            _ => false,
            _ => files,
            _ => true);

        TestAssert.Equal(@"E:\Imported\sounds\alert.mp3", audio.FilePath);
        TestAssert.Equal(@"E:\Imported\images\alert.png", image.FilePath);
        TestAssert.Equal(@"D:\Old\images\duplicate.png", duplicate.FilePath);
        TestAssert.Equal(2, result.RelinkedCount);
        TestAssert.Equal(1, result.UnresolvedCount);
        TestAssert.Equal(1, result.AmbiguousCount);
    }
}
