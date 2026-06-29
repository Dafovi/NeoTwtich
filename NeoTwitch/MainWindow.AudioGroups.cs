using System.Windows;
using NeoTwitch.Models;
using NeoTwitch.Services.Library;
using NeoTwitch.Services.Text;
using NeoTwitch.ViewModels.Activity;
using WpfMessageBox = System.Windows.MessageBox;

namespace NeoTwitch;

public partial class MainWindow
{
    internal void AddAudioGroupButton_Click(object sender, RoutedEventArgs e)
    {
        var name = NewAudioGroupNameBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            WpfMessageBox.Show(this, _text.Get(UiTextKeys.LibraryWriteGroupName), _text.Get(UiTextKeys.AudioTitle), MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var mutation = LibraryGroupService.GetOrCreate<AudioGroupConfig>(_config.AudioGroups, name);
        if (!mutation.IsValid || mutation.Group is null)
        {
            return;
        }

        if (!mutation.Created)
        {
            NewAudioGroupBox.SelectedValue = mutation.Group.Id;
            NewAudioGroupNameBox.Text = "";
            return;
        }

        NewAudioGroupBox.SelectedValue = mutation.Group.Id;
        NewAudioGroupNameBox.Text = "";

        SaveConfig();
        RefreshAudioLibraryView();
        UpdateRuleOptionVisibility();
        AddLog(_text.Format(UiTextKeys.LibraryGroupCreatedLog, _text.Get(UiTextKeys.AudioTitle), mutation.Group.Name), ActivityLogKind.Audio);
    }

    internal void ViewAudioGroupButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement element || element.Tag is not string groupId)
        {
            return;
        }

        var group = _config.AudioGroups.FirstOrDefault(item => string.Equals(item.Id, groupId, StringComparison.OrdinalIgnoreCase));
        if (group is null)
        {
            return;
        }

        _audioGroupFilterId = group.Id;
        _audioFilter = "ALL";
        _audioSearchText = "";
        _audioLibraryViewModel.SetFilters("", _audioFilter, notify: false);
        UpdateAudioFilterButtons();
        RefreshAudioLibraryView();
        AddLog(_text.Format(UiTextKeys.LibraryShowingGroupLog, _text.Get(UiTextKeys.AudioTitle), group.Name), ActivityLogKind.Audio);
    }

    internal void DeleteAudioGroupButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement element || element.Tag is not string groupId)
        {
            return;
        }

        var group = _config.AudioGroups.FirstOrDefault(item => string.Equals(item.Id, groupId, StringComparison.OrdinalIgnoreCase));
        if (group is null)
        {
            return;
        }

        var audioCount = LibraryGroupService.CountAssetsInGroup(_config.AudioLibrary, group.Id);
        if (WpfMessageBox.Show(
                this,
                _text.Format(UiTextKeys.LibraryDeleteGroupPrompt, group.Name, audioCount),
                _text.Get(UiTextKeys.AudioTitle),
                MessageBoxButton.YesNo,
                MessageBoxImage.Question) != MessageBoxResult.Yes)
        {
            return;
        }

        LibraryGroupService.ClearGroupFromAssets(_config.AudioLibrary, group.Id);
        LibraryGroupService.ClearAudioGroupFromRules(_config.Rules, group.Id);

        _config.AudioGroups.Remove(group);
        if (string.Equals(_audioGroupFilterId, group.Id, StringComparison.OrdinalIgnoreCase))
        {
            _audioGroupFilterId = "";
        }

        SaveConfig();
        RefreshAudioLibraryView();
        RefreshRulesView();
        LoadSelectedRuleIntoUi();
        AddLog(_text.Format(UiTextKeys.LibraryGroupDeletedLog, _text.Get(UiTextKeys.AudioTitle), group.Name), ActivityLogKind.Audio);
    }
}
