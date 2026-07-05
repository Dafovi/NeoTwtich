using NeoTwitch.Services.Text;
using NeoTwitch.ViewModels.Library;

namespace NeoTwitch;

public partial class MainWindow
{
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
}
