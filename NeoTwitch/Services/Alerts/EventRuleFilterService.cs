using NeoTwitch.Models;
using NeoTwitch.Services.Text;

namespace NeoTwitch.Services.Alerts;

public static class EventRuleFilterService
{
    public const string AllStatus = "ALL";
    public const string ActiveStatus = "ACTIVE";
    public const string InactiveStatus = "INACTIVE";

    public static bool Matches(EventRule rule, string statusFilter, string categoryFilter, string searchText)
    {
        return Matches(rule, statusFilter, categoryFilter, searchText, UiTextService.CreateDefault());
    }

    public static bool Matches(EventRule rule, string statusFilter, string categoryFilter, string searchText, IUiTextService textService)
    {
        if (string.Equals(statusFilter, ActiveStatus, StringComparison.OrdinalIgnoreCase) && !rule.IsEnabled)
        {
            return false;
        }

        if (string.Equals(statusFilter, InactiveStatus, StringComparison.OrdinalIgnoreCase) && rule.IsEnabled)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(categoryFilter)
            && !string.Equals(rule.EventKind.ToString(), categoryFilter, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(searchText))
        {
            return true;
        }

        var text = searchText.Trim();
        return TextSearchHelper.ContainsIgnoreCase(rule.Name, text)
            || TextSearchHelper.ContainsIgnoreCase(rule.DisplayLabel, text)
            || TextSearchHelper.ContainsIgnoreCase(rule.ChatCommand, text)
            || TextSearchHelper.ContainsIgnoreCase(rule.CustomRewardTitle, text)
            || TextSearchHelper.ContainsIgnoreCase(rule.ChatMessageTemplate, text)
            || TextSearchHelper.ContainsIgnoreCase(DisplayNameService.For(rule.EventKind, textService), text);
    }
}
