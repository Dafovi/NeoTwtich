using System.Windows;
using NeoTwitch.Models;
using NeoTwitch.Services.Library;
using NeoTwitch.Services.Text;
using NeoTwitch.ViewModels.Activity;
using NeoTwitch.ViewModels.Library;
using WpfMessageBox = System.Windows.MessageBox;

namespace NeoTwitch;

public partial class MainWindow
{
    private void AddMediaGroup(MediaLibraryKind kind)
    {
        var viewModel = GetMediaLibraryViewModel(kind);
        var title = MediaLibraryTitle(kind);
        var name = viewModel.NewGroupName.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            WpfMessageBox.Show(this, _text.Get(UiTextKeys.LibraryWriteGroupName), title, MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var groups = GetMediaGroups(kind);
        var mutation = LibraryGroupService.GetOrCreate<MediaGroupConfig>(groups, name);
        if (!mutation.IsValid || mutation.Group is null)
        {
            return;
        }

        if (!mutation.Created)
        {
            viewModel.SelectNewAssetGroup(mutation.Group.Id);
            return;
        }

        viewModel.SelectNewAssetGroup(mutation.Group.Id);
        SaveConfig();
        RefreshMediaLibraryView(kind);
        AddLog(_text.Format(UiTextKeys.LibraryGroupCreatedLog, title, mutation.Group.Name), ActivityLogKind.Info);
    }

    private void ViewMediaGroup(MediaLibraryKind kind, object? parameter)
    {
        if (parameter is not string groupId)
        {
            return;
        }

        var group = GetMediaGroups(kind).FirstOrDefault(item => string.Equals(item.Id, groupId, StringComparison.OrdinalIgnoreCase));
        if (group is null)
        {
            return;
        }

        if (kind == MediaLibraryKind.Image)
        {
            _imageGroupFilterId = group.Id;
            _imageFilter = "ALL";
            _imageSearchText = "";
            _imageLibraryViewModel.SetFilters("", _imageFilter, notify: false);
        }
        else
        {
            _videoGroupFilterId = group.Id;
            _videoFilter = "ALL";
            _videoSearchText = "";
            _videoLibraryViewModel.SetFilters("", _videoFilter, notify: false);
        }

        UpdateMediaFilterButtons(kind);
        RefreshMediaLibraryView(kind);
    }

    private void DeleteMediaGroup(MediaLibraryKind kind, object? parameter)
    {
        if (parameter is not string groupId)
        {
            return;
        }

        var groups = GetMediaGroups(kind);
        var group = groups.FirstOrDefault(item => string.Equals(item.Id, groupId, StringComparison.OrdinalIgnoreCase));
        if (group is null)
        {
            return;
        }

        var library = GetMediaLibrary(kind);
        var count = LibraryGroupService.CountAssetsInGroup(library, group.Id);
        var title = MediaLibraryTitle(kind);
        if (WpfMessageBox.Show(
                this,
                _text.Format(UiTextKeys.LibraryDeleteGroupPrompt, group.Name, count),
                title,
                MessageBoxButton.YesNo,
                MessageBoxImage.Question) != MessageBoxResult.Yes)
        {
            return;
        }

        LibraryGroupService.ClearGroupFromAssets(library, group.Id);

        groups.Remove(group);
        if (kind == MediaLibraryKind.Image && string.Equals(_imageGroupFilterId, group.Id, StringComparison.OrdinalIgnoreCase))
        {
            _imageGroupFilterId = "";
        }
        else if (kind == MediaLibraryKind.Video && string.Equals(_videoGroupFilterId, group.Id, StringComparison.OrdinalIgnoreCase))
        {
            _videoGroupFilterId = "";
        }

        SaveConfig();
        RefreshMediaLibraryView(kind);
    }
}
