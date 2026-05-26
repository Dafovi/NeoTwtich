using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace LucesCanjeTwitch.Models;

public sealed class EventRule : INotifyPropertyChanged
{
    private string _name = "";
    private bool _isEnabled = true;
    private TwitchEventKind _eventKind;
    private string _customRewardTitle = "";
    private int _minimumBits = 1;
    private bool _useLights;
    private bool _playAudio;
    private string _audioPath = "";
    private bool _sendChatMessage;
    private string _chatMessageTemplate = "";
    private bool _sendAlexaEvent;
    private string _alexaEventName = "";
    private LightPattern _pattern = LightPattern.Pulse;
    private string _targetPins = "";
    private string _primaryColor = "#FF2D55";
    private string _secondaryColor = "#00D1FF";
    private string _tertiaryColor = "#FFFFFF";
    private int _brightness = 120;
    private int _durationMs = 4500;
    private int _cycleMs = 80;
    private int _stepMs = 120;

    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string Name
    {
        get => _name;
        set => SetField(ref _name, value);
    }

    public bool IsEnabled
    {
        get => _isEnabled;
        set => SetField(ref _isEnabled, value);
    }

    public TwitchEventKind EventKind
    {
        get => _eventKind;
        set
        {
            if (SetField(ref _eventKind, value))
            {
                OnPropertyChanged(nameof(DisplayLabel));
            }
        }
    }

    public string CustomRewardTitle
    {
        get => _customRewardTitle;
        set => SetField(ref _customRewardTitle, value);
    }

    public int MinimumBits
    {
        get => _minimumBits;
        set
        {
            if (SetField(ref _minimumBits, Math.Clamp(value, 1, 1_000_000)))
            {
                OnPropertyChanged(nameof(DisplayLabel));
            }
        }
    }

    public bool UseLights
    {
        get => _useLights;
        set => SetField(ref _useLights, value);
    }

    public bool PlayAudio
    {
        get => _playAudio;
        set => SetField(ref _playAudio, value);
    }

    public string AudioPath
    {
        get => _audioPath;
        set => SetField(ref _audioPath, value);
    }

    public bool SendChatMessage
    {
        get => _sendChatMessage;
        set => SetField(ref _sendChatMessage, value);
    }

    public string ChatMessageTemplate
    {
        get => _chatMessageTemplate;
        set => SetField(ref _chatMessageTemplate, value);
    }

    public bool SendAlexaEvent
    {
        get => _sendAlexaEvent;
        set => SetField(ref _sendAlexaEvent, value);
    }

    public string AlexaEventName
    {
        get => _alexaEventName;
        set => SetField(ref _alexaEventName, value);
    }

    public LightPattern Pattern
    {
        get => _pattern;
        set => SetField(ref _pattern, value);
    }

    public string TargetPins
    {
        get => _targetPins;
        set => SetField(ref _targetPins, value);
    }

    public string PrimaryColor
    {
        get => _primaryColor;
        set => SetField(ref _primaryColor, value);
    }

    public string SecondaryColor
    {
        get => _secondaryColor;
        set => SetField(ref _secondaryColor, value);
    }

    public string TertiaryColor
    {
        get => _tertiaryColor;
        set => SetField(ref _tertiaryColor, value);
    }

    public int Brightness
    {
        get => _brightness;
        set => SetField(ref _brightness, Math.Clamp(value, 0, 255));
    }

    public int DurationMs
    {
        get => _durationMs;
        set => SetField(ref _durationMs, Math.Clamp(value, 250, 60000));
    }

    public int CycleMs
    {
        get => _cycleMs;
        set => SetField(ref _cycleMs, Math.Clamp(value, 10, 2000));
    }

    public int StepMs
    {
        get => _stepMs;
        set => SetField(ref _stepMs, Math.Clamp(value, 10, 5000));
    }

    public string DisplayLabel
    {
        get
        {
            var label = string.IsNullOrWhiteSpace(Name)
                ? DisplayNames.For(EventKind)
                : $"{Name} - {DisplayNames.For(EventKind)}";

            return EventKind == TwitchEventKind.Cheer
                ? $"{label} >= {MinimumBits} bits"
                : label;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public bool Matches(TwitchEvent twitchEvent)
    {
        if (!IsEnabled || EventKind != twitchEvent.Kind)
        {
            return false;
        }

        if (EventKind == TwitchEventKind.Cheer)
        {
            return twitchEvent.Bits is int bits && bits >= MinimumBits;
        }

        if (EventKind != TwitchEventKind.ChannelPointRedemption)
        {
            return true;
        }

        return string.IsNullOrWhiteSpace(CustomRewardTitle)
            || string.Equals(CustomRewardTitle.Trim(), twitchEvent.RewardTitle?.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    public EventRule Duplicate()
    {
        return new EventRule
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = $"{Name} copia".Trim(),
            IsEnabled = IsEnabled,
            EventKind = EventKind,
            CustomRewardTitle = CustomRewardTitle,
            MinimumBits = MinimumBits,
            UseLights = UseLights,
            PlayAudio = PlayAudio,
            AudioPath = AudioPath,
            SendChatMessage = SendChatMessage,
            ChatMessageTemplate = ChatMessageTemplate,
            SendAlexaEvent = SendAlexaEvent,
            AlexaEventName = AlexaEventName,
            Pattern = Pattern,
            TargetPins = TargetPins,
            PrimaryColor = PrimaryColor,
            SecondaryColor = SecondaryColor,
            TertiaryColor = TertiaryColor,
            Brightness = Brightness,
            DurationMs = DurationMs,
            CycleMs = CycleMs,
            StepMs = StepMs
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

        if (propertyName is nameof(Name))
        {
            OnPropertyChanged(nameof(DisplayLabel));
        }

        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
