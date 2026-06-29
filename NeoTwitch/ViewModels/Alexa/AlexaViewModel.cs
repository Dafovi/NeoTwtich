using NeoTwitch.ViewModels.Core;

namespace NeoTwitch.ViewModels.Alexa;

public sealed class AlexaViewModel : ObservableObject
{
    private Action _applyBackground = Noop;
    private Action _stopBackground = Noop;

    public AlexaViewModel()
    {
        ApplyBackgroundCommand = new RelayCommand(() => _applyBackground());
        StopBackgroundCommand = new RelayCommand(() => _stopBackground());
    }

    public RelayCommand ApplyBackgroundCommand { get; }

    public RelayCommand StopBackgroundCommand { get; }

    public void ConfigureActions(Action applyBackground, Action stopBackground)
    {
        _applyBackground = applyBackground;
        _stopBackground = stopBackground;
    }

    private static void Noop()
    {
    }
}
