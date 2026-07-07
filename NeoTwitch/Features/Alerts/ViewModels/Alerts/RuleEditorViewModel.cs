using NeoTwitch.Models;
using NeoTwitch.ViewModels.Core;

namespace NeoTwitch.ViewModels.Alerts;

public sealed class RuleEditorViewModel : ObservableObject
{
    private const double BrightnessMaximum = 255d;

    private bool _isEnabled = true;
    private string _ruleNameText = "";
    private TwitchEventKind _eventKind = TwitchEventKind.Follow;
    private string _customRewardTitle = "";
    private string _chatCommand = "";
    private string _minimumBitsText = "1";
    private bool _sendChatMessage;
    private string _chatMessageTemplate = "";
    private bool _sendAlexaEvent;
    private bool _useLights;
    private bool _playAudio;
    private bool _sendObsScene;
    private string _obsSceneName = "";
    private string _obsSceneDelayText = "0";
    private bool _obsReturnToPreviousScene = true;
    private string _obsReturnDelayText = "15000";
    private bool _sendObsMedia;
    private ObsMediaKind _obsMediaKind = ObsMediaKind.Image;
    private MediaSourceMode _obsMediaSourceMode = MediaSourceMode.Single;
    private string _obsMediaAssetId = "";
    private string _obsMediaGroupId = "";
    private string _obsMediaDurationText = "5000";
    private bool _sendObsImage;
    private MediaSourceMode _obsImageSourceMode = MediaSourceMode.Single;
    private string _obsImageAssetId = "";
    private string _obsImageGroupId = "";
    private string _obsImageDurationText = "5000";
    private bool _sendObsVideo;
    private MediaSourceMode _obsVideoSourceMode = MediaSourceMode.Single;
    private string _obsVideoAssetId = "";
    private string _obsVideoGroupId = "";
    private bool _useVirtualLights;
    private bool _virtualLightsToObs = true;
    private bool _virtualLightsToScreen;
    private string _virtualLightsScreenId = "";
    private LightPattern _virtualLightsPattern = LightPattern.Pulse;
    private string _virtualLightsPrimaryColor = "#14B8A6";
    private string _virtualLightsSecondaryColor = "#B56CFF";
    private string _virtualLightsTertiaryColor = "#FFFFFF";
    private double _virtualLightsBrightness = 180d;
    private double _virtualLightsDurationMs = 4500d;
    private double _virtualLightsCycleMs = 80d;
    private double _virtualLightsStepMs = 120d;
    private AudioSourceMode _audioSourceMode = AudioSourceMode.Single;
    private string _audioAssetId = "";
    private string _audioGroupId = "";
    private LightPattern _pattern = LightPattern.Pulse;
    private string _targetPins = "";
    private string _primaryColor = "#14B8A6";
    private string _secondaryColor = "#B56CFF";
    private string _tertiaryColor = "#FFFFFF";
    private double _brightness = 180d;
    private double _durationMs = 5000d;
    private double _cycleMs = 80d;
    private double _stepMs = 120d;

    public bool IsEnabled
    {
        get => _isEnabled;
        set => SetProperty(ref _isEnabled, value);
    }

    public string RuleNameText
    {
        get => _ruleNameText;
        set => SetProperty(ref _ruleNameText, value ?? "");
    }

    public TwitchEventKind EventKind
    {
        get => _eventKind;
        set => SetProperty(ref _eventKind, value);
    }

    public string CustomRewardTitle
    {
        get => _customRewardTitle;
        set => SetProperty(ref _customRewardTitle, value ?? "");
    }

    public string ChatCommand
    {
        get => _chatCommand;
        set => SetProperty(ref _chatCommand, value ?? "");
    }

    public string MinimumBitsText
    {
        get => _minimumBitsText;
        set => SetProperty(ref _minimumBitsText, value ?? "");
    }

    public bool SendChatMessage
    {
        get => _sendChatMessage;
        set => SetProperty(ref _sendChatMessage, value);
    }

    public string ChatMessageTemplate
    {
        get => _chatMessageTemplate;
        set => SetProperty(ref _chatMessageTemplate, value ?? "");
    }

    public bool SendAlexaEvent
    {
        get => _sendAlexaEvent;
        set => SetProperty(ref _sendAlexaEvent, value);
    }

    public bool UseLights
    {
        get => _useLights;
        set => SetProperty(ref _useLights, value);
    }

    public bool PlayAudio
    {
        get => _playAudio;
        set => SetProperty(ref _playAudio, value);
    }

    public bool SendObsScene
    {
        get => _sendObsScene;
        set => SetProperty(ref _sendObsScene, value);
    }

    public string ObsSceneName
    {
        get => _obsSceneName;
        set => SetProperty(ref _obsSceneName, value ?? "");
    }

    public string ObsSceneDelayText
    {
        get => _obsSceneDelayText;
        set => SetProperty(ref _obsSceneDelayText, value ?? "");
    }

    public bool ObsReturnToPreviousScene
    {
        get => _obsReturnToPreviousScene;
        set => SetProperty(ref _obsReturnToPreviousScene, value);
    }

    public string ObsReturnDelayText
    {
        get => _obsReturnDelayText;
        set => SetProperty(ref _obsReturnDelayText, value ?? "");
    }

    public bool SendObsMedia
    {
        get => _sendObsMedia;
        set => SetProperty(ref _sendObsMedia, value);
    }

    public ObsMediaKind ObsMediaKind
    {
        get => _obsMediaKind;
        set => SetProperty(ref _obsMediaKind, value);
    }

    public MediaSourceMode ObsMediaSourceMode
    {
        get => _obsMediaSourceMode;
        set => SetProperty(ref _obsMediaSourceMode, value);
    }

    public string ObsMediaAssetId
    {
        get => _obsMediaAssetId;
        set => SetProperty(ref _obsMediaAssetId, value ?? "");
    }

    public string ObsMediaGroupId
    {
        get => _obsMediaGroupId;
        set => SetProperty(ref _obsMediaGroupId, value ?? "");
    }

    public string ObsMediaDurationText
    {
        get => _obsMediaDurationText;
        set => SetProperty(ref _obsMediaDurationText, value ?? "");
    }

    public bool SendObsImage
    {
        get => _sendObsImage;
        set => SetProperty(ref _sendObsImage, value);
    }

    public MediaSourceMode ObsImageSourceMode
    {
        get => _obsImageSourceMode;
        set => SetProperty(ref _obsImageSourceMode, value);
    }

    public string ObsImageAssetId
    {
        get => _obsImageAssetId;
        set => SetProperty(ref _obsImageAssetId, value ?? "");
    }

    public string ObsImageGroupId
    {
        get => _obsImageGroupId;
        set => SetProperty(ref _obsImageGroupId, value ?? "");
    }

    public string ObsImageDurationText
    {
        get => _obsImageDurationText;
        set => SetProperty(ref _obsImageDurationText, value ?? "");
    }

    public bool SendObsVideo
    {
        get => _sendObsVideo;
        set => SetProperty(ref _sendObsVideo, value);
    }

    public MediaSourceMode ObsVideoSourceMode
    {
        get => _obsVideoSourceMode;
        set => SetProperty(ref _obsVideoSourceMode, value);
    }

    public string ObsVideoAssetId
    {
        get => _obsVideoAssetId;
        set => SetProperty(ref _obsVideoAssetId, value ?? "");
    }

    public string ObsVideoGroupId
    {
        get => _obsVideoGroupId;
        set => SetProperty(ref _obsVideoGroupId, value ?? "");
    }

    public bool UseVirtualLights
    {
        get => _useVirtualLights;
        set => SetProperty(ref _useVirtualLights, value);
    }

    public bool VirtualLightsToObs
    {
        get => _virtualLightsToObs;
        set => SetProperty(ref _virtualLightsToObs, value);
    }

    public bool VirtualLightsToScreen
    {
        get => _virtualLightsToScreen;
        set => SetProperty(ref _virtualLightsToScreen, value);
    }

    public string VirtualLightsScreenId
    {
        get => _virtualLightsScreenId;
        set => SetProperty(ref _virtualLightsScreenId, value ?? "");
    }

    public LightPattern VirtualLightsPattern
    {
        get => _virtualLightsPattern;
        set => SetProperty(ref _virtualLightsPattern, value);
    }

    public string VirtualLightsPrimaryColor
    {
        get => _virtualLightsPrimaryColor;
        set => SetProperty(ref _virtualLightsPrimaryColor, value ?? "");
    }

    public string VirtualLightsSecondaryColor
    {
        get => _virtualLightsSecondaryColor;
        set => SetProperty(ref _virtualLightsSecondaryColor, value ?? "");
    }

    public string VirtualLightsTertiaryColor
    {
        get => _virtualLightsTertiaryColor;
        set => SetProperty(ref _virtualLightsTertiaryColor, value ?? "");
    }

    public double VirtualLightsBrightness
    {
        get => _virtualLightsBrightness;
        set => SetProperty(ref _virtualLightsBrightness, value);
    }

    public double VirtualLightsDurationMs
    {
        get => _virtualLightsDurationMs;
        set => SetProperty(ref _virtualLightsDurationMs, value);
    }

    public double VirtualLightsCycleMs
    {
        get => _virtualLightsCycleMs;
        set => SetProperty(ref _virtualLightsCycleMs, value);
    }

    public double VirtualLightsStepMs
    {
        get => _virtualLightsStepMs;
        set => SetProperty(ref _virtualLightsStepMs, value);
    }

    public AudioSourceMode AudioSourceMode
    {
        get => _audioSourceMode;
        set => SetProperty(ref _audioSourceMode, value);
    }

    public string AudioAssetId
    {
        get => _audioAssetId;
        set => SetProperty(ref _audioAssetId, value ?? "");
    }

    public string AudioGroupId
    {
        get => _audioGroupId;
        set => SetProperty(ref _audioGroupId, value ?? "");
    }

    public LightPattern Pattern
    {
        get => _pattern;
        set => SetProperty(ref _pattern, value);
    }

    public string TargetPins
    {
        get => _targetPins;
        set => SetProperty(ref _targetPins, value ?? "");
    }

    public string PrimaryColor
    {
        get => _primaryColor;
        set => SetProperty(ref _primaryColor, value ?? "");
    }

    public string SecondaryColor
    {
        get => _secondaryColor;
        set => SetProperty(ref _secondaryColor, value ?? "");
    }

    public string TertiaryColor
    {
        get => _tertiaryColor;
        set => SetProperty(ref _tertiaryColor, value ?? "");
    }

    public double Brightness
    {
        get => _brightness;
        set
        {
            if (SetProperty(ref _brightness, value))
            {
                OnPropertyChanged(nameof(BrightnessPercent));
                OnPropertyChanged(nameof(BrightnessPercentText));
            }
        }
    }

    public int BrightnessPercent => BrightnessMaximum <= 0d
        ? 0
        : (int)Math.Round(Math.Clamp(Brightness / BrightnessMaximum, 0d, 1d) * 100d);

    public string BrightnessPercentText => $"{BrightnessPercent}%";

    public double DurationMs
    {
        get => _durationMs;
        set => SetProperty(ref _durationMs, value);
    }

    public double CycleMs
    {
        get => _cycleMs;
        set => SetProperty(ref _cycleMs, value);
    }

    public double StepMs
    {
        get => _stepMs;
        set => SetProperty(ref _stepMs, value);
    }

    public void LoadBasicFields(EventRule rule)
    {
        IsEnabled = rule.IsEnabled;
        RuleNameText = rule.Name;
        EventKind = rule.EventKind;
        CustomRewardTitle = rule.CustomRewardTitle;
        ChatCommand = rule.ChatCommand;
        MinimumBitsText = rule.MinimumBits.ToString();
        SendChatMessage = rule.SendChatMessage;
        ChatMessageTemplate = rule.ChatMessageTemplate;
        SendAlexaEvent = rule.SendAlexaEvent;
        UseLights = rule.UseLights;
        PlayAudio = rule.PlayAudio;
        SendObsScene = rule.SendObsScene;
        ObsSceneName = rule.ObsSceneName;
        ObsSceneDelayText = rule.ObsSceneDelayMs.ToString();
        ObsReturnToPreviousScene = rule.ObsReturnToPreviousScene;
        ObsReturnDelayText = rule.ObsReturnDelayMs.ToString();
        SendObsMedia = false;
        ObsMediaKind = rule.ObsMediaKind;
        ObsMediaSourceMode = rule.ObsMediaSourceMode;
        ObsMediaAssetId = "";
        ObsMediaGroupId = "";
        ObsMediaDurationText = rule.ObsMediaDurationMs.ToString();
        SendObsImage = rule.SendObsImage;
        ObsImageSourceMode = rule.ObsImageSourceMode;
        ObsImageAssetId = rule.ObsImageAssetId;
        ObsImageGroupId = rule.ObsImageGroupId;
        ObsImageDurationText = rule.ObsImageDurationMs.ToString();
        SendObsVideo = rule.SendObsVideo;
        ObsVideoSourceMode = rule.ObsVideoSourceMode;
        ObsVideoAssetId = rule.ObsVideoAssetId;
        ObsVideoGroupId = rule.ObsVideoGroupId;
        UseVirtualLights = rule.UseVirtualLights;
        VirtualLightsToObs = rule.VirtualLightsToObs;
        VirtualLightsToScreen = rule.VirtualLightsToScreen;
        VirtualLightsScreenId = rule.VirtualLightsScreenId;
        VirtualLightsPattern = rule.VirtualLightsPattern;
        VirtualLightsPrimaryColor = rule.VirtualLightsPrimaryColor;
        VirtualLightsSecondaryColor = rule.VirtualLightsSecondaryColor;
        VirtualLightsTertiaryColor = rule.VirtualLightsTertiaryColor;
        VirtualLightsBrightness = rule.VirtualLightsBrightness;
        VirtualLightsDurationMs = rule.VirtualLightsDurationMs;
        VirtualLightsCycleMs = rule.VirtualLightsCycleMs;
        VirtualLightsStepMs = rule.VirtualLightsStepMs;
        AudioSourceMode = rule.AudioSourceMode;
        AudioAssetId = rule.AudioAssetId;
        AudioGroupId = rule.AudioGroupId;
        Pattern = rule.Pattern;
        TargetPins = rule.TargetPins;
        PrimaryColor = rule.PrimaryColor;
        SecondaryColor = rule.SecondaryColor;
        TertiaryColor = rule.TertiaryColor;
        Brightness = rule.Brightness;
        DurationMs = rule.DurationMs;
        CycleMs = rule.CycleMs;
        StepMs = rule.StepMs;
    }

    public void Clear()
    {
        IsEnabled = true;
        RuleNameText = "";
        EventKind = TwitchEventKind.Follow;
        CustomRewardTitle = "";
        ChatCommand = "";
        MinimumBitsText = "1";
        SendChatMessage = false;
        ChatMessageTemplate = "";
        SendAlexaEvent = false;
        UseLights = false;
        PlayAudio = false;
        SendObsScene = false;
        ObsSceneName = "";
        ObsSceneDelayText = "0";
        ObsReturnToPreviousScene = true;
        ObsReturnDelayText = "15000";
        SendObsMedia = false;
        ObsMediaKind = ObsMediaKind.Image;
        ObsMediaSourceMode = MediaSourceMode.Single;
        ObsMediaAssetId = "";
        ObsMediaGroupId = "";
        ObsMediaDurationText = "5000";
        SendObsImage = false;
        ObsImageSourceMode = MediaSourceMode.Single;
        ObsImageAssetId = "";
        ObsImageGroupId = "";
        ObsImageDurationText = "5000";
        SendObsVideo = false;
        ObsVideoSourceMode = MediaSourceMode.Single;
        ObsVideoAssetId = "";
        ObsVideoGroupId = "";
        UseVirtualLights = false;
        VirtualLightsToObs = true;
        VirtualLightsToScreen = false;
        VirtualLightsScreenId = "";
        VirtualLightsPattern = LightPattern.Pulse;
        VirtualLightsPrimaryColor = "#14B8A6";
        VirtualLightsSecondaryColor = "#B56CFF";
        VirtualLightsTertiaryColor = "#FFFFFF";
        VirtualLightsBrightness = 180d;
        VirtualLightsDurationMs = 4500d;
        VirtualLightsCycleMs = 80d;
        VirtualLightsStepMs = 120d;
        AudioSourceMode = AudioSourceMode.Single;
        AudioAssetId = "";
        AudioGroupId = "";
        Pattern = LightPattern.Pulse;
        TargetPins = "";
        PrimaryColor = "#14B8A6";
        SecondaryColor = "#B56CFF";
        TertiaryColor = "#FFFFFF";
        Brightness = 180d;
        DurationMs = 5000d;
        CycleMs = 80d;
        StepMs = 120d;
    }
}
