using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace LucesCanjeTwitch.Models;

public sealed class LedStripConfig : INotifyPropertyChanged
{
    private string _name = "Principal";
    private int _pin = 6;
    private int _ledCount = 30;

    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string Name
    {
        get => _name;
        set
        {
            if (SetField(ref _name, value))
            {
                OnPropertyChanged(nameof(DisplayLabel));
            }
        }
    }

    public int Pin
    {
        get => _pin;
        set
        {
            if (SetField(ref _pin, Math.Clamp(value, 0, 53)))
            {
                OnPropertyChanged(nameof(DisplayLabel));
            }
        }
    }

    public int LedCount
    {
        get => _ledCount;
        set
        {
            if (SetField(ref _ledCount, Math.Clamp(value, 1, 600)))
            {
                OnPropertyChanged(nameof(DisplayLabel));
            }
        }
    }

    public string DisplayLabel => $"{Name} - pin {Pin}, {LedCount} LEDs";

    public event PropertyChangedEventHandler? PropertyChanged;

    public LedStripConfig Duplicate()
    {
        return new LedStripConfig
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = $"{Name} copia".Trim(),
            Pin = Pin,
            LedCount = LedCount
        };
    }

    public override string ToString() => DisplayLabel;

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
