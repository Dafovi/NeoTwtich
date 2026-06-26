using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using NeoTwitch.Models;
using NeoTwitch.Services;
using NeoTwitch.Services.Library;
using NeoTwitch.Services.Text;
using NeoTwitch.Services.Ui;
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
            foreach (var row in LibraryGroupRowFactoryService.CreateAudioGroupRows(_config.AudioGroups, _config.AudioLibrary))
            {
                _audioGroupRows.Add(row);
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
        return LibraryRowFactoryService.CreateAudioRow(
            audio,
            _config.Rules,
            groupsById,
            _text.Get(UiTextKeys.LibraryNoGroup),
            _previewingAudioId,
            _audioPreviewPlayback is not null,
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
            FilterButtonThemeService.Apply(button, active, "#14B8A6", palette);
        }
    }
}
