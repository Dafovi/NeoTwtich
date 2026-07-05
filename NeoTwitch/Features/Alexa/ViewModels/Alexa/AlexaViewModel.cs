using NeoTwitch.Models;
using NeoTwitch.ViewModels.Core;

namespace NeoTwitch.ViewModels.Alexa;

public sealed class AlexaViewModel : ObservableObject
{
    private Action _applyBackground = Noop;
    private Action _stopBackground = Noop;
    private bool _backgroundEnabled;
    private bool _backgroundTurnOffAfterEvent;
    private string _backgroundOnEventName = "luz_encendida";
    private string _backgroundOffEventName = "luz_apagada";

    public AlexaViewModel()
    {
        ApplyBackgroundCommand = new RelayCommand(() => _applyBackground());
        StopBackgroundCommand = new RelayCommand(() => _stopBackground());
    }

    public RelayCommand ApplyBackgroundCommand { get; }

    public RelayCommand StopBackgroundCommand { get; }

    public bool BackgroundEnabled
    {
        get => _backgroundEnabled;
        set => SetProperty(ref _backgroundEnabled, value);
    }

    public bool BackgroundTurnOffAfterEvent
    {
        get => _backgroundTurnOffAfterEvent;
        set => SetProperty(ref _backgroundTurnOffAfterEvent, value);
    }

    public string BackgroundOnEventName
    {
        get => _backgroundOnEventName;
        set => SetProperty(ref _backgroundOnEventName, value ?? "");
    }

    public string BackgroundOffEventName
    {
        get => _backgroundOffEventName;
        set => SetProperty(ref _backgroundOffEventName, value ?? "");
    }

    public void LoadBackgroundConfig(AppConfig config)
    {
        BackgroundEnabled = config.BackgroundAlexaEnabled;
        BackgroundTurnOffAfterEvent = config.BackgroundAlexaTurnOffAfterEvent;
        BackgroundOnEventName = config.BackgroundAlexaOnEventName;
        BackgroundOffEventName = config.BackgroundAlexaOffEventName;
    }

    public void ConfigureActions(Action applyBackground, Action stopBackground)
    {
        _applyBackground = applyBackground;
        _stopBackground = stopBackground;
    }

    private static void Noop()
    {
    }
}
