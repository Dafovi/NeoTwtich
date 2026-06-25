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
    internal void AddImageGroupButton_Click(object sender, RoutedEventArgs e)
    {
        AddMediaGroup(MediaLibraryKind.Image);
    }

    internal void AddVideoGroupButton_Click(object sender, RoutedEventArgs e)
    {
        AddMediaGroup(MediaLibraryKind.Video);
    }

    private void AddMediaGroup(MediaLibraryKind kind)
    {
        var nameBox = kind == MediaLibraryKind.Image ? NewImageGroupNameBox : NewVideoGroupNameBox;
        var title = MediaLibraryTitle(kind);
        var name = nameBox.Text.Trim();
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
            if (kind == MediaLibraryKind.Image)
            {
                NewImageGroupBox.SelectedValue = mutation.Group.Id;
            }
            else
            {
                NewVideoGroupBox.SelectedValue = mutation.Group.Id;
            }

            nameBox.Text = "";
            return;
        }

        if (kind == MediaLibraryKind.Image)
        {
            NewImageGroupBox.SelectedValue = mutation.Group.Id;
        }
        else
        {
            NewVideoGroupBox.SelectedValue = mutation.Group.Id;
        }

        nameBox.Text = "";
        SaveConfig();
        RefreshMediaLibraryView(kind);
        AddLog(_text.Format(UiTextKeys.LibraryGroupCreatedLog, title, mutation.Group.Name), ActivityLogKind.Info);
    }

    internal void ViewImageGroupButton_Click(object sender, RoutedEventArgs e)
    {
        ViewMediaGroup(MediaLibraryKind.Image, sender);
    }

    internal void ViewVideoGroupButton_Click(object sender, RoutedEventArgs e)
    {
        ViewMediaGroup(MediaLibraryKind.Video, sender);
    }

    private void ViewMediaGroup(MediaLibraryKind kind, object sender)
    {
        if (sender is not FrameworkElement element || element.Tag is not string groupId)
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
            ImageSearchBox.Text = "";
            _imageSearchText = "";
        }
        else
        {
            _videoGroupFilterId = group.Id;
            _videoFilter = "ALL";
            VideoSearchBox.Text = "";
            _videoSearchText = "";
        }

        UpdateMediaFilterButtons(kind);
        RefreshMediaLibraryView(kind);
    }

    internal void DeleteImageGroupButton_Click(object sender, RoutedEventArgs e)
    {
        DeleteMediaGroup(MediaLibraryKind.Image, sender);
    }

    internal void DeleteVideoGroupButton_Click(object sender, RoutedEventArgs e)
    {
        DeleteMediaGroup(MediaLibraryKind.Video, sender);
    }

    private void DeleteMediaGroup(MediaLibraryKind kind, object sender)
    {
        if (sender is not FrameworkElement element || element.Tag is not string groupId)
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
