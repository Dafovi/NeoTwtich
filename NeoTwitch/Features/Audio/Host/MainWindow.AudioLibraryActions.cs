using System.IO;
using System.Windows.Controls;
using System.Windows.Threading;
using NeoTwitch.Models;
using NeoTwitch.Services.Library;
using NeoTwitch.Services.Text;
using NeoTwitch.Services.Ui;
using NeoTwitch.ViewModels.Activity;

namespace NeoTwitch;

public partial class MainWindow
{
    internal void AudioLibraryGroupBox_DropDownClosed(object sender, EventArgs e)
    {
        if (_refreshingAudioLibrary
            || _loadingUi
            || sender is not System.Windows.Controls.ComboBox comboBox
            || comboBox.Tag is not string audioId)
        {
            return;
        }

        var audio = _config.AudioLibrary.FirstOrDefault(item => string.Equals(item.Id, audioId, StringComparison.OrdinalIgnoreCase));
        if (audio is null)
        {
            return;
        }

        var selectedGroupId = comboBox.SelectedValue as string ?? "";
        if (string.Equals(audio.GroupId, selectedGroupId, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        audio.GroupId = selectedGroupId;
        SaveConfig();
        _ = Dispatcher.InvokeAsync(() =>
        {
            RefreshAudioLibraryView();
            RefreshRulesView();
        }, DispatcherPriority.Background);
    }

    private void BrowseNewAudio()
    {
        var fileName = _filePicker.OpenFile(new FilePickerRequest(
            _text.Get(UiTextKeys.AudioTitle),
            _text.Get(UiTextKeys.AudioFileDialogFilter)));
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return;
        }

        _audioLibraryViewModel.SetNewAssetPath(fileName, Path.GetFileNameWithoutExtension(fileName));
    }

    private async void SaveNewAudio()
    {
        var path = _audioLibraryViewModel.NewAssetPath;
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            _dialog.ShowInformation(_text.Get(UiTextKeys.AudioTitle), _text.Get(UiTextKeys.AudioPickValidFile));
            return;
        }

        var result = await AudioLibraryAddService.AddOrUpdateAsync(
            _config,
            new AudioAssetAddRequest(
                path,
                _audioLibraryViewModel.NewAssetName,
                _audioLibraryViewModel.NewAssetGroupId,
                _audioLibraryViewModel.NewAssetAlertId),
            _audioPlayer.ProbeDurationAsync);
        var audio = result.Asset;
        if (audio is null)
        {
            _dialog.ShowInformation(_text.Get(UiTextKeys.AudioTitle), _text.Get(UiTextKeys.AudioPickValidFile));
            return;
        }

        if (ReferenceEquals(_alertsViewModel.SelectedRule, result.LinkedRule))
        {
            LoadSelectedRuleIntoUi();
        }

        _audioLibraryViewModel.ClearNewAssetForm();

        SaveConfig();
        RefreshAudioLibraryView();
        RefreshRulesView();
        AddLog(_text.Format(UiTextKeys.LibrarySavedLog, _text.Get(UiTextKeys.AudioTitle), audio.DisplayName), ActivityLogKind.Audio);
    }
}
