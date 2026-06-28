using System.Windows;
using System.Windows.Controls;
using NeoTwitch.Services.Library;
using NeoTwitch.Services.Text;

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
            foreach (var row in LibraryGroupRowFactoryService.CreateAudioGroupRows(
                         _config.AudioGroups,
                         _config.AudioLibrary,
                         count => _text.Format(UiTextKeys.LibraryAudioCount, count, count == 1 ? "" : "s")))
            {
                _audioGroupRows.Add(row);
            }

            var summary = LibrarySummaryService.Create(
                _config.AudioLibrary,
                _config.AudioGroups,
                rows.Length,
                _audioGroupFilterId,
                groupsById,
                _text.Get(UiTextKeys.AudioFooterNoun),
                GetLibrarySummaryLabels());
            AudioSavedCountText.Text = summary.AssetCountText;
            AudioGroupCountText.Text = summary.GroupCountText;
            LastAudioText.Text = summary.LastAssetText;
            AudioLibraryFooterText.Text = summary.FooterText;

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
}
