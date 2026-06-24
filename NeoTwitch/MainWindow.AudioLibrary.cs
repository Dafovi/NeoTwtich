using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using NeoTwitch.Models;
using NeoTwitch.Services;
using NeoTwitch.Services.Library;
using NeoTwitch.Services.Text;
using NeoTwitch.ViewModels.Activity;
using NeoTwitch.ViewModels.Library;
using WpfMessageBox = System.Windows.MessageBox;
using WpfOpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace NeoTwitch;

public partial class MainWindow
{
    internal void AudioSearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_loadingUi || sender is not System.Windows.Controls.TextBox textBox)
        {
            return;
        }

        _audioSearchText = textBox.Text.Trim();
        RefreshAudioLibraryView();
    }

    internal void AudioFilterButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button button)
        {
            return;
        }

        _audioFilter = button.Tag?.ToString() ?? "ALL";
        _audioGroupFilterId = "";
        UpdateAudioFilterButtons();
        RefreshAudioLibraryView();
    }

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

    internal void BrowseNewAudioButton_Click(object sender, RoutedEventArgs e)
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

    internal async void SaveNewAudioButton_Click(object sender, RoutedEventArgs e)
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
            if (ReferenceEquals(RulesList.SelectedItem, rule))
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

    internal void AddAudioGroupButton_Click(object sender, RoutedEventArgs e)
    {
        var name = NewAudioGroupNameBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            WpfMessageBox.Show(this, _text.Get(UiTextKeys.LibraryWriteGroupName), _text.Get(UiTextKeys.AudioTitle), MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var existing = _config.AudioGroups.FirstOrDefault(group =>
            string.Equals(group.Name, name, StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
        {
            NewAudioGroupBox.SelectedValue = existing.Id;
            NewAudioGroupNameBox.Text = "";
            return;
        }

        var group = new AudioGroupConfig { Name = name };
        _config.AudioGroups.Add(group);
        NewAudioGroupBox.SelectedValue = group.Id;
        NewAudioGroupNameBox.Text = "";

        SaveConfig();
        RefreshAudioLibraryView();
        UpdateRuleOptionVisibility();
        AddLog(_text.Format(UiTextKeys.LibraryGroupCreatedLog, _text.Get(UiTextKeys.AudioTitle), group.Name), ActivityLogKind.Audio);
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
        AudioSearchBox.Text = "";
        _audioSearchText = "";
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

        var audioCount = _config.AudioLibrary.Count(audio => string.Equals(audio.GroupId, group.Id, StringComparison.OrdinalIgnoreCase));
        if (WpfMessageBox.Show(
                this,
                _text.Format(UiTextKeys.LibraryDeleteGroupPrompt, group.Name, audioCount),
                _text.Get(UiTextKeys.AudioTitle),
                MessageBoxButton.YesNo,
                MessageBoxImage.Question) != MessageBoxResult.Yes)
        {
            return;
        }

        foreach (var audio in _config.AudioLibrary.Where(audio => string.Equals(audio.GroupId, group.Id, StringComparison.OrdinalIgnoreCase)))
        {
            audio.GroupId = "";
        }

        foreach (var rule in _config.Rules.Where(rule => rule.AudioSourceMode == AudioSourceMode.Group
                     && string.Equals(rule.AudioGroupId, group.Id, StringComparison.OrdinalIgnoreCase)))
        {
            rule.AudioGroupId = "";
            rule.PlayAudio = false;
        }

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

    internal async void PreviewAudioButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement element || element.Tag is not string audioId)
        {
            return;
        }

        var audio = _config.AudioLibrary.FirstOrDefault(item => string.Equals(item.Id, audioId, StringComparison.OrdinalIgnoreCase));
        if (audio is null)
        {
            return;
        }

        if (_audioPreviewPlayback is not null && string.Equals(_previewingAudioId, audio.Id, StringComparison.OrdinalIgnoreCase))
        {
            _audioPreviewPlayback.Stop();
            ClearAudioPreviewState(audio.Id);
            return;
        }

        var playback = await _audioPlayer.PrepareAsync(audio.FilePath, _config.AlertVolumePercent, AddLog);
        if (playback is null)
        {
            return;
        }

        _audioPreviewPlayback?.Stop();
        _audioPreviewPlayback = playback;
        _previewingAudioId = audio.Id;
        MarkAudioAssetUsed(audio, playback.Duration);
        playback.Play();
        AddLog(_text.Format(UiTextKeys.AudioPlayingLog, audio.DisplayName), ActivityLogKind.Audio);
        _ = WatchAudioPreviewCompletionAsync(playback, audio.Id);
    }

    internal void DeleteAudioButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement element || element.Tag is not string audioId)
        {
            return;
        }

        var audio = _config.AudioLibrary.FirstOrDefault(item => string.Equals(item.Id, audioId, StringComparison.OrdinalIgnoreCase));
        if (audio is null)
        {
            return;
        }

        if (WpfMessageBox.Show(this, _text.Format(UiTextKeys.LibraryDeleteAssetPrompt, audio.DisplayName), _text.Get(UiTextKeys.AudioTitle), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
        {
            return;
        }

        if (string.Equals(_previewingAudioId, audio.Id, StringComparison.OrdinalIgnoreCase))
        {
            _audioPreviewPlayback?.Stop();
            ClearAudioPreviewState(audio.Id);
        }

        _config.AudioLibrary.Remove(audio);
        foreach (var rule in _config.Rules.Where(rule => string.Equals(rule.AudioAssetId, audio.Id, StringComparison.OrdinalIgnoreCase)))
        {
            rule.AudioAssetId = "";
            rule.AudioPath = "";
            rule.PlayAudio = rule.AudioSourceMode == AudioSourceMode.Group && !string.IsNullOrWhiteSpace(rule.AudioGroupId);
        }

        SaveConfig();
        RefreshAudioLibraryView();
        RefreshRulesView();
        LoadSelectedRuleIntoUi();
    }

    private async Task WatchAudioPreviewCompletionAsync(AudioPlayback playback, string audioId)
    {
        try
        {
            await playback.Completion;
        }
        finally
        {
            await Dispatcher.InvokeAsync(() => ClearAudioPreviewState(audioId));
        }
    }

    private void ClearAudioPreviewState(string audioId)
    {
        if (!string.Equals(_previewingAudioId, audioId, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        _audioPreviewPlayback = null;
        _previewingAudioId = "";
        RefreshAudioLibraryView();
    }

    private void StopAudioPreview()
    {
        if (_audioPreviewPlayback is null)
        {
            return;
        }

        var audioId = _previewingAudioId;
        _audioPreviewPlayback.Stop();
        ClearAudioPreviewState(audioId);
    }

    private bool RuleHasValidAudio(EventRule rule)
    {
        if (!Dispatcher.CheckAccess())
        {
            return Dispatcher.Invoke(() => RuleHasValidAudio(rule));
        }

        return AudioRuleAssetService.HasValidAudio(rule, _config.AudioLibrary, _audioRandom);
    }

    private AudioAssetConfig? ResolveRuleAudioAsset(EventRule rule)
    {
        if (!Dispatcher.CheckAccess())
        {
            return Dispatcher.Invoke(() => ResolveRuleAudioAsset(rule));
        }

        return AudioRuleAssetService.ResolveRuleAudioAsset(rule, _config.AudioLibrary, _audioRandom);
    }

    private void MarkAudioAssetUsed(AudioAssetConfig audio, TimeSpan? duration)
    {
        if (!Dispatcher.CheckAccess())
        {
            _ = Dispatcher.InvokeAsync(() => MarkAudioAssetUsed(audio, duration));
            return;
        }

        if (duration is { TotalMilliseconds: > 0 })
        {
            audio.DurationMs = (int)Math.Round(duration.Value.TotalMilliseconds);
        }

        audio.LastUsedAt = DateTimeOffset.Now;
        SaveConfig();
        RefreshAudioLibraryView();
    }

    private void RefreshAudioLibraryView()
    {
        if (_initializingComponent)
        {
            return;
        }

        if (!Dispatcher.CheckAccess())
        {
            _ = Dispatcher.InvokeAsync(RefreshAudioLibraryView);
            return;
        }

        _refreshingAudioLibrary = true;
        try
        {
            var groupsById = _config.AudioGroups.ToDictionary(group => group.Id, group => group.Name, StringComparer.OrdinalIgnoreCase);

            RefreshAudioGroupChoicesIfNeeded();
            RefreshAudioAlertChoicesIfNeeded();

            var rows = _config.AudioLibrary
                .Select((audio, index) => CreateAudioLibraryRow(audio, groupsById, index))
                .Where(AudioRowMatchesFilters)
                .ToArray();

            _audioLibraryRows.Clear();
            foreach (var row in rows)
            {
                _audioLibraryRows.Add(row);
            }

            _audioGroupRows.Clear();
            var groupIndex = 0;
            foreach (var group in _config.AudioGroups)
            {
                var count = _config.AudioLibrary.Count(audio => string.Equals(audio.GroupId, group.Id, StringComparison.OrdinalIgnoreCase));
                _audioGroupRows.Add(new AudioGroupRow(
                    group.Id,
                    group.Name,
                    $"{count} audio{(count == 1 ? "" : "s")}",
                    FrozenBrushFrom((groupIndex++ % 4) switch
                    {
                        0 => "#14B8A6",
                        1 => "#B56CFF",
                        2 => "#37C7F3",
                        _ => "#22C55E"
                    })));
            }

            AudioSavedCountText.Text = _config.AudioLibrary.Count.ToString();
            AudioGroupCountText.Text = _config.AudioGroups.Count.ToString();
            var lastAudio = _config.AudioLibrary
                .Where(audio => audio.LastUsedAt is not null)
                .OrderByDescending(audio => audio.LastUsedAt)
                .FirstOrDefault();
            LastAudioText.Text = lastAudio?.DisplayName ?? "Sin uso";
            var groupFilterText = string.IsNullOrWhiteSpace(_audioGroupFilterId)
                ? ""
                : $" del grupo {groupsById.GetValueOrDefault(_audioGroupFilterId, "seleccionado")}";
            AudioLibraryFooterText.Text = $"Mostrando {rows.Length} de {_config.AudioLibrary.Count} audios{groupFilterText}";

            RuleAudioAssetBox.Items.Refresh();
            RuleAudioGroupBox.Items.Refresh();
            NewAudioAlertBox.Items.Refresh();
            NewAudioGroupBox.Items.Refresh();
            UpdateAudioFilterButtons();
        }
        finally
        {
            _refreshingAudioLibrary = false;
        }
    }

    private void RefreshAudioGroupChoicesIfNeeded()
    {
        var signature = string.Join("|", _config.AudioGroups.Select(group => $"{group.Id}:{group.Name}"));
        if (string.Equals(signature, _audioGroupChoicesSignature, StringComparison.Ordinal))
        {
            return;
        }

        AudioGroupChoices.Clear();
        AudioGroupChoices.Add(new AudioGroupChoice("", _text.Get(UiTextKeys.LibraryNoGroup)));
        foreach (var group in _config.AudioGroups)
        {
            AudioGroupChoices.Add(new AudioGroupChoice(group.Id, group.Name));
        }

        _audioGroupChoicesSignature = signature;
    }

    private void RefreshAudioAlertChoicesIfNeeded()
    {
        var signature = string.Join("|", _config.Rules.Select(rule => $"{rule.Id}:{rule.Name}"));
        if (string.Equals(signature, _audioAlertChoicesSignature, StringComparison.Ordinal))
        {
            return;
        }

        AudioAlertChoices.Clear();
        AudioAlertChoices.Add(new AudioAlertChoice("", _text.Get(UiTextKeys.LibraryNoAlertAssigned)));
        foreach (var rule in _config.Rules)
        {
            AudioAlertChoices.Add(new AudioAlertChoice(rule.Id, string.IsNullOrWhiteSpace(rule.Name) ? rule.DisplayLabel : rule.Name));
        }

        _audioAlertChoicesSignature = signature;
    }

    private AudioLibraryRow CreateAudioLibraryRow(AudioAssetConfig audio, IReadOnlyDictionary<string, string> groupsById, int index)
    {
        var assignedRules = _config.Rules
            .Where(rule => AudioRuleAssetService.RuleUsesAudioAsset(rule, audio))
            .ToArray();
        var assignedText = assignedRules.Length switch
        {
            0 => "",
            1 => assignedRules[0].Name,
            _ => $"{assignedRules[0].Name} +{assignedRules.Length - 1}"
        };
        var accentColor = assignedRules.Length > 0
            ? EventKindAccent(assignedRules[0].EventKind)
            : "#64748B";

        return new AudioLibraryRow(
            audio.Id,
            audio.DisplayName,
            audio.FilePath,
            audio.GroupId,
            assignedText,
            groupsById.TryGetValue(audio.GroupId, out var groupName) ? groupName : _text.Get(UiTextKeys.LibraryNoGroup),
            audio.DurationText,
            assignedRules.Length > 0,
            string.Equals(_previewingAudioId, audio.Id, StringComparison.OrdinalIgnoreCase) && _audioPreviewPlayback is not null,
            FrozenBrushFrom(accentColor),
            TranslucentBrushFrom(accentColor),
            index);
    }

    private bool AudioRowMatchesFilters(AudioLibraryRow row)
    {
        return LibraryRowFilterService.MatchesAudio(
            row,
            _audioGroupFilterId,
            _audioFilter,
            _audioSearchText,
            _text.Get(UiTextKeys.LibraryNoGroup));
    }

    private void UpdateAudioFilterButtons()
    {
        if (_initializingComponent)
        {
            return;
        }

        var palette = _config.DarkMode ? ThemePalette.Dark : ThemePalette.Light;
        foreach (var button in new[] { AudioFilterAllButton, AudioFilterWithAlertButton, AudioFilterNoGroupButton })
        {
            var active = string.Equals(button.Tag?.ToString(), _audioFilter, StringComparison.OrdinalIgnoreCase);
            button.Background = active ? TranslucentBrushFrom("#14B8A6") : palette.Input;
            button.Foreground = active ? FrozenBrushFrom("#14B8A6") : palette.Text;
            button.BorderBrush = active ? FrozenBrushFrom("#14B8A6") : palette.Border;
        }
    }
}
