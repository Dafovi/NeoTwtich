using NeoTwitch.Models;
using NeoTwitch.Services.Ui;

namespace NeoTwitch;

public partial class MainWindow
{
    private void UpdateEventKindTileSelection()
    {
        if (_initializingComponent)
        {
            return;
        }

        var selectedKind = _alertsViewModel.SelectedCategoryKind;
        var palette = _config.DarkMode
            ? ThemePalette.Dark
            : ThemePalette.Light;

        foreach (var button in EventKindTileButtons())
        {
            if (button.Tag is not string value || !Enum.TryParse<TwitchEventKind>(value, out var tileKind))
            {
                continue;
            }

            var selected = tileKind == selectedKind;
            SelectionButtonThemeService.Apply(
                button,
                selected,
                UiAccentCatalog.ForEventKind(tileKind),
                palette);
        }
    }

    private IEnumerable<System.Windows.Controls.Button> EventKindTileButtons()
    {
        return
        [
            EventFollowTileButton,
            EventSubscriptionTileButton,
            EventRaidTileButton,
            EventCheerTileButton,
            EventChatCommandTileButton,
            EventRedemptionTileButton
        ];
    }
}
