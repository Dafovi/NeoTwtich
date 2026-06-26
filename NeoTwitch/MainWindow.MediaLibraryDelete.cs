using System.Windows;
using NeoTwitch.Services.Text;
using NeoTwitch.ViewModels.Library;
using WpfMessageBox = System.Windows.MessageBox;

namespace NeoTwitch;

public partial class MainWindow
{
    private void DeleteMediaAsset(MediaLibraryKind kind, object sender)
    {
        if (sender is not FrameworkElement element || element.Tag is not string assetId)
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
        if (WpfMessageBox.Show(this, _text.Format(UiTextKeys.LibraryDeleteAssetPrompt, asset.DisplayName), title, MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
        {
            return;
        }

        library.Remove(asset);
        SaveConfig();
        RefreshMediaLibraryView(kind);
    }
}
