using NeoTwitch.Models;
using NeoTwitch.Services.Text;

namespace NeoTwitch.Services.Alerts;

public sealed class RuleSimulationService
{
    private readonly IUiTextService _text;

    public RuleSimulationService(IUiTextService text)
    {
        _text = text;
    }

    public TwitchEvent BuildEvent(EventRule rule)
    {
        var kind = rule.EventKind == TwitchEventKind.Test
            ? TwitchEventKind.Follow
            : rule.EventKind;
        var userName = _text.Get(UiTextKeys.RuleSimulationUserName);
        var bits = Math.Max(1, rule.MinimumBits);
        var viewers = 18;
        var rewardTitle = FirstNonEmpty(rule.CustomRewardTitle, _text.Get(UiTextKeys.RuleSimulationRewardTitle));
        var message = kind == TwitchEventKind.ChatCommand
            ? FirstNonEmpty(rule.ChatCommand, _text.Get(UiTextKeys.RuleSimulationChatCommandMessage))
            : _text.Get(UiTextKeys.RuleSimulationMessage);

        return new TwitchEvent
        {
            Kind = kind,
            UserName = userName,
            RewardTitle = kind == TwitchEventKind.ChannelPointRedemption ? rewardTitle : null,
            Bits = kind == TwitchEventKind.Cheer ? bits : null,
            ViewerCount = kind == TwitchEventKind.Raid ? viewers : null,
            Message = kind == TwitchEventKind.ChatCommand ? message : _text.Get(UiTextKeys.RuleSimulationMessage),
            RawType = "simulator",
            Title = _text.Format(UiTextKeys.RuleSimulationTitle, DisplayNameService.For(kind, _text), userName)
        };
    }

    public static bool MatchesChatCommand(EventRule rule, string? message)
    {
        if (rule.EventKind != TwitchEventKind.ChatCommand)
        {
            return true;
        }

        var command = rule.ChatCommand.Trim();
        if (string.IsNullOrWhiteSpace(command))
        {
            return false;
        }

        if (!command.StartsWith('!'))
        {
            command = $"!{command}";
        }

        var firstToken = message?.Trim().Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        return string.Equals(firstToken, command, StringComparison.OrdinalIgnoreCase);
    }

    public string DescribeEvent(TwitchEvent twitchEvent)
    {
        var user = FirstNonEmpty(twitchEvent.UserName ?? "", _text.Get(UiTextKeys.RuleSimulationUserName));
        return twitchEvent.Kind switch
        {
            TwitchEventKind.Cheer => _text.Format(UiTextKeys.RuleSimulationDescribeCheer, twitchEvent.Bits ?? 0, user),
            TwitchEventKind.Raid => _text.Format(UiTextKeys.RuleSimulationDescribeRaid, user, twitchEvent.ViewerCount ?? 0),
            TwitchEventKind.ChannelPointRedemption => _text.Format(
                UiTextKeys.RuleSimulationDescribeRedemption,
                FirstNonEmpty(twitchEvent.RewardTitle ?? "", _text.Get(UiTextKeys.RuleSimulationRewardTitle)),
                user),
            TwitchEventKind.ChatCommand => _text.Format(
                UiTextKeys.RuleSimulationDescribeChatCommand,
                user,
                FirstNonEmpty(twitchEvent.Message ?? "", _text.Get(UiTextKeys.RuleSimulationNoMessage))),
            _ => _text.Format(UiTextKeys.RuleSimulationDescribeDefault, DisplayNameService.For(twitchEvent.Kind, _text), user)
        };
    }

    public string DescribeActions(EventRule rule)
    {
        List<string> actions = [];

        if (rule.UseLights)
        {
            actions.Add(_text.Get(UiTextKeys.RuleSimulationActionLights));
        }

        if (rule.PlayAudio)
        {
            actions.Add(_text.Get(UiTextKeys.RuleSimulationActionAudio));
        }

        if (rule.SendChatMessage)
        {
            actions.Add(_text.Get(UiTextKeys.RuleSimulationActionChat));
        }

        if (rule.SendAlexaEvent)
        {
            actions.Add(_text.Get(UiTextKeys.RuleSimulationActionAlexa));
        }

        return actions.Count == 0 ? _text.Get(UiTextKeys.RuleSimulationNoActions) : string.Join(", ", actions);
    }

    private static string FirstNonEmpty(params string[] values)
    {
        return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim() ?? "";
    }
}
