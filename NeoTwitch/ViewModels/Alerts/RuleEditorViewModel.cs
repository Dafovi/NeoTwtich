using NeoTwitch.Models;
using NeoTwitch.ViewModels.Core;

namespace NeoTwitch.ViewModels.Alerts;

public sealed class RuleEditorViewModel : ObservableObject
{
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
    private AudioSourceMode _audioSourceMode = AudioSourceMode.Single;
    private string _audioAssetId = "";
    private string _audioGroupId = "";

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
        SendObsMedia = rule.SendObsMedia;
        ObsMediaKind = rule.ObsMediaKind;
        ObsMediaSourceMode = rule.ObsMediaSourceMode;
        ObsMediaAssetId = rule.ObsMediaAssetId;
        ObsMediaGroupId = rule.ObsMediaGroupId;
        ObsMediaDurationText = rule.ObsMediaDurationMs.ToString();
        AudioSourceMode = rule.AudioSourceMode;
        AudioAssetId = rule.AudioAssetId;
        AudioGroupId = rule.AudioGroupId;
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
        AudioSourceMode = AudioSourceMode.Single;
        AudioAssetId = "";
        AudioGroupId = "";
    }
}
