using NeoTwitch.Services.Text;
using NeoTwitch.Services.Library;
using NeoTwitch.ViewModels.Library;

namespace NeoTwitch;

public partial class MainWindow
{
    private void DeleteMediaAsset(MediaLibraryKind kind, object? parameter)
    {
        if (parameter is not string assetId)
        {
            return;
        }

        var library = GetMediaLibrary(kind);
        var asset = library.FirstOrDefault(item => string.Equals(item.Id, assetId, StringComparison.OrdinalIgnoreCase));
        if (asset is null)
        {
            return;
        }

        var title = MediaLibraryTitle(kind);
        if (!_dialog.Confirm(title, _text.Format(UiTextKeys.LibraryDeleteAssetPrompt, asset.DisplayName)))
        {
            return;
        }

        MediaLibraryMutationService.RemoveMediaAsset(_config, kind, asset.Id);
        SaveConfig();
        RefreshMediaLibraryView(kind);
    }
}
