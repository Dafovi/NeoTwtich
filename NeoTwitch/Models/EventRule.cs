using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using NeoTwitch.Services.Alerts;

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
    private AudioSourceMode _audioSourceMode = AudioSourceMode.Single;
    private string _audioAssetId = "";
    private string _audioGroupId = "";
    private bool _sendChatMessage;
    private string _chatMessageTemplate = "";
    private bool _sendAlexaEvent;
    private string _alexaEventName = "";
    private bool _sendObsScene;
    private string _obsSceneName = "";
    private int _obsSceneDelayMs;
    private bool _obsReturnToPreviousScene = true;
    private int _obsReturnDelayMs = 15000;
    private bool _sendObsMedia;
    private ObsMediaKind _obsMediaKind = ObsMediaKind.Image;
    private MediaSourceMode _obsMediaSourceMode = MediaSourceMode.Single;
    private string _obsMediaAssetId = "";
    private string _obsMediaGroupId = "";
    private int _obsMediaDurationMs = 5000;
    private LightPattern _pattern = LightPattern.Pulse;
    private string _targetPins = "";
    private string _primaryColor = "#FF2D55";
    private string _secondaryColor = "#00D1FF";
    private string _tertiaryColor = "#FFFFFF";
    private int _brightness = 120;
    private int _durationMs = 4500;
    private int _cycleMs = 80;
    private int _stepMs = 120;
    private bool _lightsActionAvailable = true;
    private bool _alexaActionAvailable = true;
    private bool _obsActionAvailable = true;

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

    public AudioSourceMode AudioSourceMode
    {
        get => _audioSourceMode;
        set => SetField(ref _audioSourceMode, value);
    }

    public string AudioAssetId
    {
        get => _audioAssetId;
        set => SetField(ref _audioAssetId, value);
    }

    public string AudioGroupId
    {
        get => _audioGroupId;
        set => SetField(ref _audioGroupId, value);
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

    public bool SendObsScene
    {
        get => _sendObsScene;
        set
        {
            if (SetField(ref _sendObsScene, value))
            {
                OnPropertyChanged(nameof(ActionsSummary));
                OnPropertyChanged(nameof(ObsActionVisibility));
            }
        }
    }

    public string ObsSceneName
    {
        get => _obsSceneName;
        set => SetField(ref _obsSceneName, value);
    }

    public int ObsSceneDelayMs
    {
        get => _obsSceneDelayMs;
        set => SetField(ref _obsSceneDelayMs, Math.Clamp(value, 0, ApplicationLimits.MaxAlertDurationMs));
    }

    public bool ObsReturnToPreviousScene
    {
        get => _obsReturnToPreviousScene;
        set => SetField(ref _obsReturnToPreviousScene, value);
    }

    public int ObsReturnDelayMs
    {
        get => _obsReturnDelayMs;
        set => SetField(ref _obsReturnDelayMs, Math.Clamp(value, 0, ApplicationLimits.MaxAlertDurationMs));
    }

    public bool SendObsMedia
    {
        get => _sendObsMedia;
        set
        {
            if (SetField(ref _sendObsMedia, value))
            {
                OnPropertyChanged(nameof(ActionsSummary));
                OnPropertyChanged(nameof(ObsActionVisibility));
            }
        }
    }

    public ObsMediaKind ObsMediaKind
    {
        get => _obsMediaKind;
        set => SetField(ref _obsMediaKind, value);
    }

    public MediaSourceMode ObsMediaSourceMode
    {
        get => _obsMediaSourceMode;
        set => SetField(ref _obsMediaSourceMode, value);
    }

    public string ObsMediaAssetId
    {
        get => _obsMediaAssetId;
        set => SetField(ref _obsMediaAssetId, value);
    }

    public string ObsMediaGroupId
    {
        get => _obsMediaGroupId;
        set => SetField(ref _obsMediaGroupId, value);
    }

    public int ObsMediaDurationMs
    {
        get => _obsMediaDurationMs;
        set => SetField(ref _obsMediaDurationMs, Math.Clamp(value, ApplicationLimits.MinAlertDurationMs, ApplicationLimits.MaxAlertDurationMs));
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
        set => SetField(ref _brightness, Math.Clamp(value, ApplicationLimits.MinBrightness, ApplicationLimits.MaxBrightness));
    }

    public int DurationMs
    {
        get => _durationMs;
        set => SetField(ref _durationMs, Math.Clamp(value, ApplicationLimits.MinAlertDurationMs, ApplicationLimits.MaxLegacyAlertDurationMs));
    }

    public int CycleMs
    {
        get => _cycleMs;
        set => SetField(ref _cycleMs, Math.Clamp(value, ApplicationLimits.MinCycleMs, ApplicationLimits.MaxCycleMs));
    }

    public int StepMs
    {
        get => _stepMs;
        set => SetField(ref _stepMs, Math.Clamp(value, ApplicationLimits.MinStepMs, ApplicationLimits.MaxStepMs));
    }

    public bool LightsActionAvailable
    {
        get => _lightsActionAvailable;
        set
        {
            if (SetField(ref _lightsActionAvailable, value))
            {
                OnPropertyChanged(nameof(LightsActionOpacity));
                OnPropertyChanged(nameof(LightsActionToolTip));
            }
        }
    }

    public bool AlexaActionAvailable
    {
        get => _alexaActionAvailable;
        set
        {
            if (SetField(ref _alexaActionAvailable, value))
            {
                OnPropertyChanged(nameof(AlexaActionOpacity));
                OnPropertyChanged(nameof(AlexaActionToolTip));
            }
        }
    }

    public bool ObsActionAvailable
    {
        get => _obsActionAvailable;
        set
        {
            if (SetField(ref _obsActionAvailable, value))
            {
                OnPropertyChanged(nameof(ObsActionOpacity));
                OnPropertyChanged(nameof(ObsActionToolTip));
            }
        }
    }

    public string DisplayLabel => EventRulePresentationService.BuildDisplayLabel(this);

    public string StatusText => EventRulePresentationService.BuildStatusText(this);

    public string StatusColor => EventRulePresentationService.BuildStatusColor(this);

    public string EventIconPath => EventRulePresentationService.BuildEventIconPath(this);

    public string EventAccentColor => EventRulePresentationService.BuildEventAccentColor(this);

    public string ActionsSummary => EventRulePresentationService.BuildActionsSummary(this);

    public Visibility LightsActionVisibility => UseLights ? Visibility.Visible : Visibility.Collapsed;

    public double LightsActionOpacity => LightsActionAvailable ? 1d : 0.32d;

    public string LightsActionToolTip => EventRulePresentationService.BuildLightsToolTip(this);

    public Visibility AudioActionVisibility => PlayAudio ? Visibility.Visible : Visibility.Collapsed;

    public Visibility ChatActionVisibility => SendChatMessage ? Visibility.Visible : Visibility.Collapsed;

    public Visibility AlexaActionVisibility => SendAlexaEvent ? Visibility.Visible : Visibility.Collapsed;

    public double AlexaActionOpacity => AlexaActionAvailable ? 1d : 0.32d;

    public string AlexaActionToolTip => EventRulePresentationService.BuildAlexaToolTip(this);

    public Visibility ObsActionVisibility => SendObsScene || SendObsMedia ? Visibility.Visible : Visibility.Collapsed;

    public double ObsActionOpacity => ObsActionAvailable ? 1d : 0.32d;

    public string ObsActionToolTip => EventRulePresentationService.BuildObsToolTip(this);

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
            AudioSourceMode = AudioSourceMode,
            AudioAssetId = AudioAssetId,
            AudioGroupId = AudioGroupId,
            SendChatMessage = SendChatMessage,
            ChatMessageTemplate = ChatMessageTemplate,
            SendAlexaEvent = SendAlexaEvent,
            AlexaEventName = AlexaEventName,
            SendObsScene = SendObsScene,
            ObsSceneName = ObsSceneName,
            ObsSceneDelayMs = ObsSceneDelayMs,
            ObsReturnToPreviousScene = ObsReturnToPreviousScene,
            ObsReturnDelayMs = ObsReturnDelayMs,
            SendObsMedia = SendObsMedia,
            ObsMediaKind = ObsMediaKind,
            ObsMediaSourceMode = ObsMediaSourceMode,
            ObsMediaAssetId = ObsMediaAssetId,
            ObsMediaGroupId = ObsMediaGroupId,
            ObsMediaDurationMs = ObsMediaDurationMs,
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

public enum AudioSourceMode
{
    Single,
    Group
}

public enum MediaSourceMode
{
    Single,
    Group
}
