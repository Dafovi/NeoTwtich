using NeoTwitch.Services.Text;
using NeoTwitch.ViewModels.Library;

namespace NeoTwitch.Services.Library;

public static class LibraryRowFilterService
{
    public const string AllFilter = "ALL";
    public const string AudioWithAlertFilter = "WITH_ALERT";
    public const string AudioNoGroupFilter = "NO_GROUP";
    public const string MediaWithGroupFilter = "WITH_GROUP";
    public const string MediaNoGroupFilter = "NO_GROUP";

    public static bool MatchesAudio(
        AudioLibraryRow row,
        string groupFilterId,
        string filter,
        string searchText,
        string noGroupText)
    {
        if (!string.IsNullOrWhiteSpace(groupFilterId)
            && !string.Equals(row.GroupId, groupFilterId, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (string.Equals(filter, AudioWithAlertFilter, StringComparison.OrdinalIgnoreCase) && !row.HasAssignedAlert)
        {
            return false;
        }

        if (string.Equals(filter, AudioNoGroupFilter, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(row.GroupName, noGroupText, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(searchText))
        {
            return true;
        }

        return TextSearchHelper.ContainsIgnoreCase(row.Name, searchText)
            || TextSearchHelper.ContainsIgnoreCase(row.FilePath, searchText)
            || TextSearchHelper.ContainsIgnoreCase(row.AssignedAlertText, searchText)
            || TextSearchHelper.ContainsIgnoreCase(row.GroupName, searchText);
    }

    public static bool MatchesMedia(
        MediaLibraryRow row,
        string groupFilterId,
        string filter,
        string searchText)
    {
        if (!string.IsNullOrWhiteSpace(groupFilterId)
            && !string.Equals(row.GroupId, groupFilterId, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (string.Equals(filter, MediaWithGroupFilter, StringComparison.OrdinalIgnoreCase)
            && string.IsNullOrWhiteSpace(row.GroupId))
        {
            return false;
        }

        if (string.Equals(filter, MediaNoGroupFilter, StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(row.GroupId))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(searchText))
        {
            return true;
        }

        return TextSearchHelper.ContainsIgnoreCase(row.Name, searchText)
            || TextSearchHelper.ContainsIgnoreCase(row.FilePath, searchText)
            || TextSearchHelper.ContainsIgnoreCase(row.GroupName, searchText)
            || TextSearchHelper.ContainsIgnoreCase(row.MetadataText, searchText);
    }
}
