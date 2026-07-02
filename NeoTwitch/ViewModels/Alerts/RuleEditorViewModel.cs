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
    }
}
