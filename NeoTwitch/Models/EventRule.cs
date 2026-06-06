using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace NeoTwitch.Models;

public sealed class EventRule : INotifyPropertyChanged
{
    private string _name = "";
    private bool _isEnabled = true;
    private TwitchEventKind _eventKind;
    private string _customRewardTitle = "";
    private string _chatCommand = "";
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
        set
        {
            if (SetField(ref _isEnabled, value))
            {
                OnPropertyChanged(nameof(StatusText));
                OnPropertyChanged(nameof(StatusColor));
            }
        }
    }

    public TwitchEventKind EventKind
    {
        get => _eventKind;
        set
        {
            if (SetField(ref _eventKind, value))
            {
                OnPropertyChanged(nameof(DisplayLabel));
                OnPropertyChanged(nameof(EventIconPath));
                OnPropertyChanged(nameof(EventAccentColor));
            }
        }
    }

    public string CustomRewardTitle
    {
        get => _customRewardTitle;
        set => SetField(ref _customRewardTitle, value);
    }

    public string ChatCommand
    {
        get => _chatCommand;
        set
        {
            if (SetField(ref _chatCommand, NormalizeCommand(value)))
            {
                OnPropertyChanged(nameof(DisplayLabel));
            }
        }
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
        set
        {
            if (SetField(ref _useLights, value))
            {
                OnPropertyChanged(nameof(ActionsSummary));
                OnPropertyChanged(nameof(LightsActionVisibility));
            }
        }
    }

    public bool PlayAudio
    {
        get => _playAudio;
        set
        {
            if (SetField(ref _playAudio, value))
            {
                OnPropertyChanged(nameof(ActionsSummary));
                OnPropertyChanged(nameof(AudioActionVisibility));
            }
        }
    }

    public string AudioPath
    {
        get => _audioPath;
        set => SetField(ref _audioPath, value);
    }

    public bool SendChatMessage
    {
        get => _sendChatMessage;
        set
        {
            if (SetField(ref _sendChatMessage, value))
            {
                OnPropertyChanged(nameof(ActionsSummary));
                OnPropertyChanged(nameof(ChatActionVisibility));
            }
        }
    }

    public string ChatMessageTemplate
    {
        get => _chatMessageTemplate;
        set => SetField(ref _chatMessageTemplate, value);
    }

    public bool SendAlexaEvent
    {
        get => _sendAlexaEvent;
        set
        {
            if (SetField(ref _sendAlexaEvent, value))
            {
                OnPropertyChanged(nameof(ActionsSummary));
                OnPropertyChanged(nameof(AlexaActionVisibility));
            }
        }
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

            return EventKind switch
            {
                TwitchEventKind.Cheer => $"{label} >= {MinimumBits} bits",
                TwitchEventKind.ChatCommand when !string.IsNullOrWhiteSpace(ChatCommand) => $"{label} ({ChatCommand})",
                _ => label
            };
        }
    }

    public string StatusText => IsEnabled ? "Activa" : "Inactiva";

    public string StatusColor => IsEnabled ? "#22C55E" : "#94A3B8";

    public string EventIconPath => EventKind switch
    {
        TwitchEventKind.Follow => "Assets/Icons/action_follower_teal.png",
        TwitchEventKind.Subscription => "Assets/Icons/action_subscription_purple.png",
        TwitchEventKind.Cheer => "Assets/Icons/action_bits_blue.png",
        TwitchEventKind.ChatCommand => "Assets/Icons/action_message_green.png",
        TwitchEventKind.ChannelPointRedemption => "Assets/Icons/activity_notification_lime.png",
        TwitchEventKind.Raid => "Assets/Icons/activity_notification.png",
        _ => "Assets/Icons/nav_rules.png"
    };

    public string EventAccentColor => EventKind switch
    {
        TwitchEventKind.Follow => "#14B8A6",
        TwitchEventKind.Subscription => "#B56CFF",
        TwitchEventKind.Raid => "#F43F5E",
        TwitchEventKind.Cheer => "#37C7F3",
        TwitchEventKind.ChatCommand => "#22C55E",
        TwitchEventKind.ChannelPointRedemption => "#FB923C",
        _ => "#94A3B8"
    };

    public string ActionsSummary
    {
        get
        {
            var actions = new List<string>();
            if (UseLights)
            {
                actions.Add("Luces");
            }

            if (PlayAudio)
            {
                actions.Add("Audio");
            }

            if (SendChatMessage)
            {
                actions.Add("Chat");
            }

            if (SendAlexaEvent)
            {
                actions.Add("Alexa");
            }

            return actions.Count == 0
                ? "Sin acciones"
                : string.Join(" / ", actions);
        }
    }

    public Visibility LightsActionVisibility => UseLights ? Visibility.Visible : Visibility.Collapsed;

    public Visibility AudioActionVisibility => PlayAudio ? Visibility.Visible : Visibility.Collapsed;

    public Visibility ChatActionVisibility => SendChatMessage ? Visibility.Visible : Visibility.Collapsed;

    public Visibility AlexaActionVisibility => SendAlexaEvent ? Visibility.Visible : Visibility.Collapsed;

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

        if (EventKind == TwitchEventKind.ChatCommand)
        {
            return MatchesChatCommand(twitchEvent.Message, ChatCommand);
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
            ChatCommand = ChatCommand,
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

    private static bool MatchesChatCommand(string? message, string command)
    {
        if (string.IsNullOrWhiteSpace(command) || string.IsNullOrWhiteSpace(message))
        {
            return false;
        }

        var firstToken = message.Trim().Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        return string.Equals(firstToken, NormalizeCommand(command), StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeCommand(string? value)
    {
        var command = value?.Trim() ?? "";
        return string.IsNullOrWhiteSpace(command)
            ? ""
            : command.StartsWith('!') ? command : $"!{command}";
    }

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
