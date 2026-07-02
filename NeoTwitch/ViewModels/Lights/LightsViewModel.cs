using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using NeoTwitch.Services.Dashboard;
using NeoTwitch.ViewModels.Ui;
using NeoTwitch.ViewModels.Core;

namespace NeoTwitch.ViewModels.Lights;

public sealed class LightsViewModel : ObservableObject
{
    private readonly CollectionViewSource _ledStripsViewSource = new();
    private string _arduinoDeviceText = "";
    private string _arduinoPortText = "";
    private string _arduinoLedCountText = "";
    private string _arduinoPinsText = "";
    private IEnumerable? _backgroundPatternChoices;
    private Action _addStrip = Noop;
    private Action _duplicateStrip = Noop;
    private Action _removeStrip = Noop;
    private Action _applyBackground = Noop;
    private Action _stopBackground = Noop;
    private Action _openSketch = Noop;
    private Action _openGuide = Noop;
    private Action<object?> _selectBackgroundPattern = Noop;
    private Action<object?> _adjustBackgroundLightValue = Noop;
    private Action<object?> _selectBackgroundLightPreset = Noop;
    private Action<object?> _pickBackgroundLightColor = Noop;

    public LightsViewModel()
    {
        AddStripCommand = new RelayCommand(() => _addStrip());
        DuplicateStripCommand = new RelayCommand(() => _duplicateStrip());
        RemoveStripCommand = new RelayCommand(() => _removeStrip());
        ApplyBackgroundCommand = new RelayCommand(() => _applyBackground());
        StopBackgroundCommand = new RelayCommand(() => _stopBackground());
        OpenSketchCommand = new RelayCommand(() => _openSketch());
        OpenGuideCommand = new RelayCommand(() => _openGuide());
        SelectBackgroundPatternCommand = new RelayCommand(parameter => _selectBackgroundPattern(parameter));
        AdjustBackgroundLightValueCommand = new RelayCommand(parameter => _adjustBackgroundLightValue(parameter));
        SelectBackgroundLightPresetCommand = new RelayCommand(parameter => _selectBackgroundLightPreset(parameter));
        PickBackgroundLightColorCommand = new RelayCommand(parameter => _pickBackgroundLightColor(parameter));
    }

    public RelayCommand AddStripCommand { get; }

    public RelayCommand DuplicateStripCommand { get; }

    public RelayCommand RemoveStripCommand { get; }

    public RelayCommand ApplyBackgroundCommand { get; }

    public RelayCommand StopBackgroundCommand { get; }

    public RelayCommand OpenSketchCommand { get; }

    public RelayCommand OpenGuideCommand { get; }

    public RelayCommand SelectBackgroundPatternCommand { get; }

    public RelayCommand AdjustBackgroundLightValueCommand { get; }

    public RelayCommand SelectBackgroundLightPresetCommand { get; }

    public RelayCommand PickBackgroundLightColorCommand { get; }

    public ICollectionView LedStripsView => _ledStripsViewSource.View;

    public ObservableCollection<RuleLedPreviewDot> BackgroundLedPreviewDots { get; } = [];

    public string ArduinoDeviceText
    {
        get => _arduinoDeviceText;
        private set => SetProperty(ref _arduinoDeviceText, value);
    }

    public string ArduinoPortText
    {
        get => _arduinoPortText;
        private set => SetProperty(ref _arduinoPortText, value);
    }

    public string ArduinoLedCountText
    {
        get => _arduinoLedCountText;
        private set => SetProperty(ref _arduinoLedCountText, value);
    }

    public string ArduinoPinsText
    {
        get => _arduinoPinsText;
        private set => SetProperty(ref _arduinoPinsText, value);
    }

    public IEnumerable? BackgroundPatternChoices
    {
        get => _backgroundPatternChoices;
        private set => SetProperty(ref _backgroundPatternChoices, value);
    }

    public void ConfigureActions(
        Action addStrip,
        Action duplicateStrip,
        Action removeStrip,
        Action applyBackground,
        Action stopBackground,
        Action openSketch,
        Action openGuide)
    {
        _addStrip = addStrip;
        _duplicateStrip = duplicateStrip;
        _removeStrip = removeStrip;
        _applyBackground = applyBackground;
        _stopBackground = stopBackground;
        _openSketch = openSketch;
        _openGuide = openGuide;
    }

    public void ConfigureEditorActions(
        Action<object?> selectBackgroundPattern,
        Action<object?> adjustBackgroundLightValue,
        Action<object?> selectBackgroundLightPreset,
        Action<object?> pickBackgroundLightColor)
    {
        _selectBackgroundPattern = selectBackgroundPattern;
        _adjustBackgroundLightValue = adjustBackgroundLightValue;
        _selectBackgroundLightPreset = selectBackgroundLightPreset;
        _pickBackgroundLightColor = pickBackgroundLightColor;
    }

    public void UpdateArduinoStatus(LightsArduinoStatusText status)
    {
        ArduinoDeviceText = status.Device;
        ArduinoPortText = status.Port;
        ArduinoLedCountText = status.LedCount;
        ArduinoPinsText = status.Pins;
    }

    public void UpdateBackgroundPatternChoices(IEnumerable? choices)
    {
        BackgroundPatternChoices = choices;
    }

    public void SetLedStripsSource(IEnumerable? ledStrips)
    {
        _ledStripsViewSource.Source = ledStrips;
        OnPropertyChanged(nameof(LedStripsView));
    }

    public void RefreshLedStrips()
    {
        _ledStripsViewSource.View.Refresh();
    }

    private static void Noop()
    {
    }

    private static void Noop(object? _)
    {
    }
}
