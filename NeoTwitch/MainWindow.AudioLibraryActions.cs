using System.IO;
using System.Windows.Controls;
using System.Windows.Threading;
using NeoTwitch.Models;
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
            "Audio|*.wav;*.mp3;*.wma;*.aac;*.m4a|Todos los archivos|*.*"));
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

        var existing = _config.AudioLibrary.FirstOrDefault(audio =>
            string.Equals(audio.FilePath, path, StringComparison.OrdinalIgnoreCase));
        var audio = existing ?? new AudioAssetConfig { FilePath = path };
        audio.Name = string.IsNullOrWhiteSpace(_audioLibraryViewModel.NewAssetName)
            ? Path.GetFileNameWithoutExtension(path)
            : _audioLibraryViewModel.NewAssetName.Trim();
        audio.GroupId = _audioLibraryViewModel.NewAssetGroupId;

        var duration = await _audioPlayer.ProbeDurationAsync(path);
        if (duration is { TotalMilliseconds: > 0 })
        {
            audio.DurationMs = (int)Math.Round(duration.Value.TotalMilliseconds);
        }

        if (existing is null)
        {
            _config.AudioLibrary.Add(audio);
        }

        var selectedRuleId = _audioLibraryViewModel.NewAssetAlertId;
        var rule = _config.Rules.FirstOrDefault(item => string.Equals(item.Id, selectedRuleId, StringComparison.OrdinalIgnoreCase));
        if (rule is not null)
        {
            rule.PlayAudio = true;
            rule.AudioSourceMode = AudioSourceMode.Single;
            rule.AudioAssetId = audio.Id;
            rule.AudioGroupId = "";
            rule.AudioPath = audio.FilePath;
            if (ReferenceEquals(_alertsViewModel.SelectedRule, rule))
            {
                LoadSelectedRuleIntoUi();
            }
        }

        _audioLibraryViewModel.ClearNewAssetForm();

        SaveConfig();
        RefreshAudioLibraryView();
        RefreshRulesView();
        AddLog(_text.Format(UiTextKeys.LibrarySavedLog, _text.Get(UiTextKeys.AudioTitle), audio.DisplayName), ActivityLogKind.Audio);
    }
}
