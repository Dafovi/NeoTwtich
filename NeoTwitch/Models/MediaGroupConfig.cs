using System.ComponentModel;
using System.Runtime.CompilerServices;
using NeoTwitch.Models.Library;

namespace NeoTwitch.Models;

public sealed class MediaGroupConfig : ILibraryGroupConfig, INotifyPropertyChanged
{
    private string _name = "";

    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string Name
    {
        get => _name;
        set => SetField(ref _name, value);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public override string ToString() => string.IsNullOrWhiteSpace(Name) ? "Grupo de media" : Name;

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
