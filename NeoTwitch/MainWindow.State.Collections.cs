using System.Collections.ObjectModel;
using System.Windows.Threading;
using NeoTwitch.ViewModels.Library;
using NeoTwitch.ViewModels.Obs;
using NeoTwitch.ViewModels.Ui;

namespace NeoTwitch;

public partial class MainWindow
{
    private LibraryScreenViewModel<AudioLibraryRow, AudioGroupRow> _audioLibraryViewModel = null!;
    private LibraryScreenViewModel<MediaLibraryRow, MediaGroupRow> _imageLibraryViewModel = null!;
    private LibraryScreenViewModel<MediaLibraryRow, MediaGroupRow> _videoLibraryViewModel = null!;
    private ObservableCollection<ObsSceneRow> _obsSceneRows => _obsViewModel.SceneRows;
    private readonly ObservableCollection<ObsSceneChoice> _obsSceneChoices = [];
    private ObservableCollection<RuleLedPreviewDot> _ruleLedPreviewDots => _alertsViewModel.LedPreviewDots;
    private ObservableCollection<RuleLedPreviewDot> _backgroundLedPreviewDots => _lightsViewModel.BackgroundLedPreviewDots;
    private readonly DispatcherTimer _ruleLedPreviewTimer = new();
    private readonly DispatcherTimer _backgroundLedPreviewTimer = new();
    private readonly DispatcherTimer _arduinoMonitorTimer = new();

    private ObservableCollection<AudioGroupChoice> AudioGroupChoices { get; } = [];

    private ObservableCollection<AudioAlertChoice> AudioAlertChoices { get; } = [];

    private ObservableCollection<MediaGroupChoice> ImageGroupChoices { get; } = [];

    private ObservableCollection<MediaGroupChoice> VideoGroupChoices { get; } = [];
}
