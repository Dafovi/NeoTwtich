using NeoTwitch.Models;

namespace NeoTwitch.Services.Alerts;

public static class RuleSimulationService
{
    public static TwitchEvent BuildEvent(EventRule rule)
    {
        var kind = rule.EventKind == TwitchEventKind.Test
            ? TwitchEventKind.Follow
            : rule.EventKind;
        var userName = "Prueba";
        var bits = Math.Max(1, rule.MinimumBits);
        var viewers = 18;
        var rewardTitle = FirstNonEmpty(rule.CustomRewardTitle, "Canje de prueba");
        var message = kind == TwitchEventKind.ChatCommand
            ? FirstNonEmpty(rule.ChatCommand, "!baile mensaje de prueba")
            : "Mensaje de prueba";

        return new TwitchEvent
        {
            Kind = kind,
            UserName = userName,
            RewardTitle = kind == TwitchEventKind.ChannelPointRedemption ? rewardTitle : null,
            Bits = kind == TwitchEventKind.Cheer ? bits : null,
            ViewerCount = kind == TwitchEventKind.Raid ? viewers : null,
            Message = kind == TwitchEventKind.ChatCommand ? message : "Mensaje de prueba",
            RawType = "simulator",
            Title = $"Simulacion: {DisplayNames.For(kind)} de {userName}"
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

    public static string DescribeEvent(TwitchEvent twitchEvent)
    {
        var user = FirstNonEmpty(twitchEvent.UserName ?? "", "Prueba");
        return twitchEvent.Kind switch
        {
            TwitchEventKind.Cheer => $"{twitchEvent.Bits ?? 0} bits de {user}",
            TwitchEventKind.Raid => $"raid de {user} con {twitchEvent.ViewerCount ?? 0} viewers",
            TwitchEventKind.ChannelPointRedemption => $"canje '{FirstNonEmpty(twitchEvent.RewardTitle ?? "", "Canje de prueba")}' de {user}",
            TwitchEventKind.ChatCommand => $"comando de chat de {user}: {FirstNonEmpty(twitchEvent.Message ?? "", "sin mensaje")}",
            _ => $"{DisplayNames.For(twitchEvent.Kind)} de {user}"
        };
    }

    public static string DescribeActions(EventRule rule)
    {
        List<string> actions = [];

        if (rule.UseLights)
        {
            actions.Add("luces");
        }

        if (rule.PlayAudio)
        {
            actions.Add("audio");
        }

        if (rule.SendChatMessage)
        {
            actions.Add("chat");
        }

        if (rule.SendAlexaEvent)
        {
            actions.Add("Alexa");
        }

        return actions.Count == 0 ? "ninguna accion activa" : string.Join(", ", actions);
    }

    private static string FirstNonEmpty(params string[] values)
    {
        return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim() ?? "";
    }
}
