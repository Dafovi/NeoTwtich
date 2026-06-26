using NeoTwitch.Models;
using NeoTwitch.Services.Library;
using NeoTwitch.Services.Text;
using NeoTwitch.Services.Ui;
using NeoTwitch.ViewModels.Library;

namespace NeoTwitch;

public partial class MainWindow
{
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
