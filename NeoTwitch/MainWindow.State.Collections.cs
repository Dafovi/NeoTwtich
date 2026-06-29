using System.Collections.ObjectModel;
using System.Windows.Data;
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
    private readonly ObservableCollection<RuleLedPreviewDot> _ruleLedPreviewDots = [];
    private readonly ObservableCollection<RuleLedPreviewDot> _backgroundLedPreviewDots = [];
    private readonly CollectionViewSource _rulesViewSource = new();
    private readonly DispatcherTimer _ruleLedPreviewTimer = new();
    private readonly DispatcherTimer _backgroundLedPreviewTimer = new();
    private readonly DispatcherTimer _arduinoMonitorTimer = new();

    public ObservableCollection<AudioGroupChoice> AudioGroupChoices { get; } = [];

    public ObservableCollection<AudioAlertChoice> AudioAlertChoices { get; } = [];

    public ObservableCollection<MediaGroupChoice> ImageGroupChoices { get; } = [];

    public ObservableCollection<MediaGroupChoice> VideoGroupChoices { get; } = [];
}
