using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using NeoTwitch.Models;
using NeoTwitch.Services.Dashboard;
using NeoTwitch.ViewModels.Ui;
using NeoTwitch.ViewModels.Core;

namespace NeoTwitch.ViewModels.Lights;

public sealed class LightsViewModel : ObservableObject
{
    private const double BrightnessMaximum = 255d;

    private readonly CollectionViewSource _ledStripsViewSource = new();
    private string _arduinoDeviceText = "";
    private string _arduinoPortText = "";
    private string _arduinoLedCountText = "";
    private string _arduinoPinsText = "";
    private bool _isStripEditorEnabled;
    private string _selectedStripName = "";
    private string _selectedStripPinText = "";
    private string _selectedStripLedCountText = "";
    private LedStripConfig? _selectedStrip;
    private IEnumerable? _backgroundPatternChoices;
    private bool _backgroundEnabled;
    private string _backgroundTargetPins = "";
    private LightPattern _backgroundPattern = LightPattern.Solid;
    private string _backgroundPrimaryColor = "#14B8A6";
    private string _backgroundSecondaryColor = "#B56CFF";
    private string _backgroundTertiaryColor = "#FFFFFF";
    private double _backgroundBrightness = 40d;
    private double _backgroundCycleMs = 120d;
    private double _backgroundStepMs = 400d;
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

    public event EventHandler? SelectedStripChanged;

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

    public bool IsStripEditorEnabled
    {
        get => _isStripEditorEnabled;
        private set => SetProperty(ref _isStripEditorEnabled, value);
    }

    public string SelectedStripName
    {
        get => _selectedStripName;
        set => SetProperty(ref _selectedStripName, value ?? "");
    }

    public string SelectedStripPinText
    {
        get => _selectedStripPinText;
        set => SetProperty(ref _selectedStripPinText, value ?? "");
    }

    public string SelectedStripLedCountText
    {
        get => _selectedStripLedCountText;
        set => SetProperty(ref _selectedStripLedCountText, value ?? "");
    }

    public LedStripConfig? SelectedStrip
    {
        get => _selectedStrip;
        set
        {
            if (SetProperty(ref _selectedStrip, value))
            {
                SelectedStripChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    public IEnumerable? BackgroundPatternChoices
    {
        get => _backgroundPatternChoices;
        private set => SetProperty(ref _backgroundPatternChoices, value);
    }

    public bool BackgroundEnabled
    {
        get => _backgroundEnabled;
        set => SetProperty(ref _backgroundEnabled, value);
    }

    public string BackgroundTargetPins
    {
        get => _backgroundTargetPins;
        set => SetProperty(ref _backgroundTargetPins, value ?? "");
    }

    public LightPattern BackgroundPattern
    {
        get => _backgroundPattern;
        set => SetProperty(ref _backgroundPattern, value);
    }

    public string BackgroundPrimaryColor
    {
        get => _backgroundPrimaryColor;
        set => SetProperty(ref _backgroundPrimaryColor, value ?? "");
    }

    public string BackgroundSecondaryColor
    {
        get => _backgroundSecondaryColor;
        set => SetProperty(ref _backgroundSecondaryColor, value ?? "");
    }

    public string BackgroundTertiaryColor
    {
        get => _backgroundTertiaryColor;
        set => SetProperty(ref _backgroundTertiaryColor, value ?? "");
    }

    public double BackgroundBrightness
    {
        get => _backgroundBrightness;
        set
        {
            if (SetProperty(ref _backgroundBrightness, value))
            {
                OnPropertyChanged(nameof(BackgroundBrightnessPercent));
                OnPropertyChanged(nameof(BackgroundBrightnessPercentText));
            }
        }
    }

    public int BackgroundBrightnessPercent => BrightnessMaximum <= 0d
        ? 0
        : (int)Math.Round(Math.Clamp(BackgroundBrightness / BrightnessMaximum, 0d, 1d) * 100d);

    public string BackgroundBrightnessPercentText => $"{BackgroundBrightnessPercent}%";

    public double BackgroundCycleMs
    {
        get => _backgroundCycleMs;
        set => SetProperty(ref _backgroundCycleMs, value);
    }

    public double BackgroundStepMs
    {
        get => _backgroundStepMs;
        set => SetProperty(ref _backgroundStepMs, value);
    }

    public void LoadBackground(AppConfig config)
    {
        BackgroundEnabled = config.BackgroundEnabled;
        BackgroundTargetPins = config.BackgroundTargetPins;
        BackgroundPattern = config.BackgroundPattern;
        BackgroundPrimaryColor = config.BackgroundPrimaryColor;
        BackgroundSecondaryColor = config.BackgroundSecondaryColor;
        BackgroundTertiaryColor = config.BackgroundTertiaryColor;
        BackgroundBrightness = config.BackgroundBrightness;
        BackgroundCycleMs = config.BackgroundCycleMs;
        BackgroundStepMs = config.BackgroundStepMs;
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

    public void LoadSelectedStrip(LedStripConfig? strip)
    {
        IsStripEditorEnabled = strip is not null;

        if (strip is null)
        {
            SelectedStripName = "";
            SelectedStripPinText = "";
            SelectedStripLedCountText = "";
            return;
        }

        SelectedStripName = strip.Name;
        SelectedStripPinText = strip.Pin.ToString();
        SelectedStripLedCountText = strip.LedCount.ToString();
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
