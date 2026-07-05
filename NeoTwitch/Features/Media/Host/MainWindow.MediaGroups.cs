using NeoTwitch.Models;
using NeoTwitch.Services.Library;
using NeoTwitch.Services.Text;
using NeoTwitch.ViewModels.Activity;
using NeoTwitch.ViewModels.Library;

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
            _dialog.ShowInformation(title, _text.Get(UiTextKeys.LibraryWriteGroupName));
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

        var viewModel = GetMediaLibraryViewModel(kind);
        viewModel.SetGroupFilter(group.Id);
        viewModel.SetFilters("", LibraryScreenViewModel<MediaLibraryRow, MediaGroupRow>.AllFilter, notify: false, clearGroupFilter: false);

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
        if (!_dialog.Confirm(
                title,
                _text.Format(UiTextKeys.LibraryDeleteGroupPrompt, group.Name, count)))
        {
            return;
        }

        LibraryGroupService.ClearGroupFromAssets(library, group.Id);
        LibraryGroupService.ClearMediaGroupFromRules(_config.Rules, MediaLibraryKindCatalog.Get(kind).ObsKind, group.Id);

        groups.Remove(group);
        var viewModel = GetMediaLibraryViewModel(kind);
        if (string.Equals(viewModel.GroupFilterId, group.Id, StringComparison.OrdinalIgnoreCase))
        {
            viewModel.SetGroupFilter("");
        }

        SaveConfig();
        RefreshMediaLibraryView(kind);
        RefreshRulesView();
        LoadSelectedRuleIntoUi();
    }
}
