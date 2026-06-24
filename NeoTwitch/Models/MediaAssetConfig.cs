using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using NeoTwitch.Models.Library;

namespace NeoTwitch.Models;

public sealed class MediaAssetConfig : ILibraryAssetConfig, INotifyPropertyChanged
{
    private string _name = "";
    private string _filePath = "";
    private string _groupId = "";
    private int _durationMs;
    private int _width;
    private int _height;
    private DateTimeOffset? _lastUsedAt;

    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string Name
    {
        get => _name;
        set
        {
            if (SetField(ref _name, value))
            {
                OnPropertyChanged(nameof(DisplayName));
            }
        }
    }

    public string FilePath
    {
        get => _filePath;
        set
        {
            if (SetField(ref _filePath, value))
            {
                OnPropertyChanged(nameof(DisplayName));
            }
        }
    }

    public string GroupId
    {
        get => _groupId;
        set => SetField(ref _groupId, value);
    }

    public int DurationMs
    {
        get => _durationMs;
        set
        {
            if (SetField(ref _durationMs, Math.Clamp(value, 0, ApplicationLimits.MaxMediaDurationMs)))
            {
                OnPropertyChanged(nameof(DurationText));
            }
        }
    }

    public int Width
    {
        get => _width;
        set
        {
            if (SetField(ref _width, Math.Clamp(value, 0, ApplicationLimits.MaxMediaDimensionPx)))
            {
                OnPropertyChanged(nameof(ResolutionText));
            }
        }
    }

    public int Height
    {
        get => _height;
        set
        {
            if (SetField(ref _height, Math.Clamp(value, 0, ApplicationLimits.MaxMediaDimensionPx)))
            {
                OnPropertyChanged(nameof(ResolutionText));
            }
        }
    }

    public DateTimeOffset? LastUsedAt
    {
        get => _lastUsedAt;
        set => SetField(ref _lastUsedAt, value);
    }

    public string DisplayName
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(Name))
            {
                return Name;
            }

            return string.IsNullOrWhiteSpace(FilePath)
                ? "Media sin nombre"
                : Path.GetFileNameWithoutExtension(FilePath);
        }
    }

    public string DurationText
    {
        get
        {
            if (DurationMs <= 0)
            {
                return "--:--";
            }

            var duration = TimeSpan.FromMilliseconds(DurationMs);
            return duration.TotalHours >= 1
                ? duration.ToString(@"h\:mm\:ss")
                : duration.ToString(@"mm\:ss");
        }
    }

    public string ResolutionText => Width > 0 && Height > 0
        ? $"{Width} x {Height}"
        : "Sin datos";

    public event PropertyChangedEventHandler? PropertyChanged;

    public override string ToString() => DisplayName;

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
