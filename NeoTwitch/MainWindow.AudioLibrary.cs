using System.Windows;
using System.Windows.Controls;
using NeoTwitch.Services.Library;
using NeoTwitch.Services.Text;

namespace NeoTwitch;

public partial class MainWindow
{
    private void AudioLibraryFiltersChanged(object? sender, EventArgs e)
    {
        if (_loadingUi)
        {
            return;
        }

        _audioSearchText = _audioLibraryViewModel.SearchText.Trim();
        _audioFilter = _audioLibraryViewModel.Filter;
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

            _audioLibraryViewModel.ReplaceAssetRows(rows);

            _audioLibraryViewModel.ReplaceGroupRows(LibraryGroupRowFactoryService.CreateAudioGroupRows(
                _config.AudioGroups,
                _config.AudioLibrary,
                count => _text.Format(UiTextKeys.LibraryAudioCount, count, count == 1 ? "" : "s")));

            var summary = LibrarySummaryService.Create(
                _config.AudioLibrary,
                _config.AudioGroups,
                rows.Length,
                _audioGroupFilterId,
                groupsById,
                _text.Get(UiTextKeys.AudioFooterNoun),
                GetLibrarySummaryLabels());
            _audioLibraryViewModel.UpdateSummary(summary);

            RuleAudioAssetBox.Items.Refresh();
            RuleAudioGroupBox.Items.Refresh();
            UpdateAudioFilterButtons();
        }
        finally
        {
            _refreshingAudioLibrary = false;
        }
    }
}
