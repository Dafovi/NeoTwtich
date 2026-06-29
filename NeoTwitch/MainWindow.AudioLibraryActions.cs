using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using NeoTwitch.Models;
using NeoTwitch.Services.Text;
using NeoTwitch.ViewModels.Activity;
using WpfMessageBox = System.Windows.MessageBox;
using WpfOpenFileDialog = Microsoft.Win32.OpenFileDialog;

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
        var dialog = new WpfOpenFileDialog
        {
            Filter = "Audio|*.wav;*.mp3;*.wma;*.aac;*.m4a|Todos los archivos|*.*",
            CheckFileExists = true
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        _newAudioPath = dialog.FileName;
        NewAudioPathBox.Text = dialog.FileName;
        if (string.IsNullOrWhiteSpace(NewAudioNameBox.Text))
        {
            NewAudioNameBox.Text = Path.GetFileNameWithoutExtension(dialog.FileName);
        }
    }

    private async void SaveNewAudio()
    {
        if (string.IsNullOrWhiteSpace(_newAudioPath) || !File.Exists(_newAudioPath))
        {
            WpfMessageBox.Show(this, _text.Get(UiTextKeys.AudioPickValidFile), _text.Get(UiTextKeys.AudioTitle), MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var existing = _config.AudioLibrary.FirstOrDefault(audio =>
            string.Equals(audio.FilePath, _newAudioPath, StringComparison.OrdinalIgnoreCase));
        var audio = existing ?? new AudioAssetConfig { FilePath = _newAudioPath };
        audio.Name = string.IsNullOrWhiteSpace(NewAudioNameBox.Text)
            ? Path.GetFileNameWithoutExtension(_newAudioPath)
            : NewAudioNameBox.Text.Trim();
        audio.GroupId = NewAudioGroupBox.SelectedValue as string ?? "";

        var duration = await _audioPlayer.ProbeDurationAsync(_newAudioPath);
        if (duration is { TotalMilliseconds: > 0 })
        {
            audio.DurationMs = (int)Math.Round(duration.Value.TotalMilliseconds);
        }

        if (existing is null)
        {
            _config.AudioLibrary.Add(audio);
        }

        var selectedRuleId = NewAudioAlertBox.SelectedValue as string ?? "";
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

        NewAudioPathBox.Text = "";
        NewAudioNameBox.Text = "";
        NewAudioAlertBox.SelectedValue = "";
        NewAudioGroupBox.SelectedValue = "";
        _newAudioPath = "";

        SaveConfig();
        RefreshAudioLibraryView();
        RefreshRulesView();
        AddLog(_text.Format(UiTextKeys.LibrarySavedLog, _text.Get(UiTextKeys.AudioTitle), audio.DisplayName), ActivityLogKind.Audio);
    }
}
