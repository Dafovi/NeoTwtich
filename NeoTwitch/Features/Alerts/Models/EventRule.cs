using System.ComponentModel;
using System.Runtime.CompilerServices;

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
    private bool _sendObsImage;
    private MediaSourceMode _obsImageSourceMode = MediaSourceMode.Single;
    private string _obsImageAssetId = "";
    private string _obsImageGroupId = "";
    private int _obsImageDurationMs = 5000;
    private bool _sendObsVideo;
    private MediaSourceMode _obsVideoSourceMode = MediaSourceMode.Single;
    private string _obsVideoAssetId = "";
    private string _obsVideoGroupId = "";
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
        set => SetField(ref _isEnabled, value);
    }

    public TwitchEventKind EventKind
    {
        get => _eventKind;
        set => SetField(ref _eventKind, value);
    }

    public string CustomRewardTitle
    {
        get => _customRewardTitle;
        set => SetField(ref _customRewardTitle, value);
    }

    public string ChatCommand
    {
        get => _chatCommand;
        set => SetField(ref _chatCommand, NormalizeCommand(value));
    }

    public int MinimumBits
    {
        get => _minimumBits;
        set => SetField(ref _minimumBits, Math.Clamp(value, 1, 1_000_000));
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

    public bool SendObsScene
    {
        get => _sendObsScene;
        set => SetField(ref _sendObsScene, value);
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
        set => SetField(ref _sendObsMedia, value);
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

    public bool SendObsImage
    {
        get => _sendObsImage;
        set => SetField(ref _sendObsImage, value);
    }

    public MediaSourceMode ObsImageSourceMode
    {
        get => _obsImageSourceMode;
        set => SetField(ref _obsImageSourceMode, value);
    }

    public string ObsImageAssetId
    {
        get => _obsImageAssetId;
        set => SetField(ref _obsImageAssetId, value);
    }

    public string ObsImageGroupId
    {
        get => _obsImageGroupId;
        set => SetField(ref _obsImageGroupId, value);
    }

    public int ObsImageDurationMs
    {
        get => _obsImageDurationMs;
        set => SetField(ref _obsImageDurationMs, Math.Clamp(value, ApplicationLimits.MinAlertDurationMs, ApplicationLimits.MaxAlertDurationMs));
    }

    public bool SendObsVideo
    {
        get => _sendObsVideo;
        set => SetField(ref _sendObsVideo, value);
    }

    public MediaSourceMode ObsVideoSourceMode
    {
        get => _obsVideoSourceMode;
        set => SetField(ref _obsVideoSourceMode, value);
    }

    public string ObsVideoAssetId
    {
        get => _obsVideoAssetId;
        set => SetField(ref _obsVideoAssetId, value);
    }

    public string ObsVideoGroupId
    {
        get => _obsVideoGroupId;
        set => SetField(ref _obsVideoGroupId, value);
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
        set => SetField(ref _lightsActionAvailable, value);
    }

    public bool AlexaActionAvailable
    {
        get => _alexaActionAvailable;
        set => SetField(ref _alexaActionAvailable, value);
    }

    public bool ObsActionAvailable
    {
        get => _obsActionAvailable;
        set => SetField(ref _obsActionAvailable, value);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public override string ToString()
    {
        return string.IsNullOrWhiteSpace(Name) ? EventKind.ToString() : Name;
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
