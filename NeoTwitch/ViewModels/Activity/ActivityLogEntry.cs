using System.Windows;
using System.Windows.Media;
using NeoTwitch.Services.Activity;

namespace NeoTwitch.ViewModels.Activity;

public sealed class ActivityLogEntry
{
    public ActivityLogEntry(string message, ActivityLogKind kind, DateTimeOffset createdAt)
    {
        var presentation = ActivityLogPresentationService.Build(message, kind);

        Kind = kind;
        Time = createdAt.ToString("HH:mm");
        Message = message;
        SourceKey = presentation.SourceKey;
        FilterKey = presentation.FilterKey;
        SourceName = presentation.SourceName;
        Category = presentation.Category;
        Title = presentation.Title;
        Description = presentation.Description;
        IsImportant = presentation.IsImportant;
        StatusText = presentation.StatusText;

        SourceBrush = ActivityLogVisuals.FrozenBrushFrom(presentation.SourceAccentColor);
        SourceBackgroundBrush = ActivityLogVisuals.TranslucentBrushFrom(presentation.SourceAccentColor);
        SourceIconImageSource = ActivityLogVisuals.LoadIcon(presentation.SourceIconPath);
        SourceImageVisibility = SourceIconImageSource is null
            ? Visibility.Collapsed
            : Visibility.Visible;
        SourceVectorVisibility = SourceIconImageSource is null
            ? Visibility.Visible
            : Visibility.Collapsed;
        SourceIconGeometry = Geometry.Parse(ActivityLogVisuals.IconData(presentation.SourceIconKey));

        StatusBrush = ActivityLogVisuals.FrozenBrushFrom(presentation.StatusAccentColor);
        StatusBackgroundBrush = ActivityLogVisuals.TranslucentBrushFrom(presentation.StatusAccentColor);
        StatusIconImageSource = ActivityLogVisuals.LoadIcon(presentation.StatusIconPath);

        AccentBrush = ActivityLogVisuals.FrozenBrushFrom(presentation.AccentColor);
        IconBackgroundBrush = ActivityLogVisuals.BackgroundBrushFrom(presentation.AccentColor);
        IconImageSource = ActivityLogVisuals.LoadIcon(presentation.ActivityIconPath);
        ImageIconVisibility = IconImageSource is null
            ? Visibility.Collapsed
            : Visibility.Visible;
        OriginalImageIconVisibility = presentation.ActivityIconUsesOriginalImage && IconImageSource is not null
            ? Visibility.Visible
            : Visibility.Collapsed;
        TintedImageIconVisibility = !presentation.ActivityIconUsesOriginalImage && IconImageSource is not null
            ? Visibility.Visible
            : Visibility.Collapsed;
        VectorIconVisibility = IconImageSource is null
            ? Visibility.Visible
            : Visibility.Collapsed;
        IconGeometry = Geometry.Parse(ActivityLogVisuals.IconData(presentation.ActivityIconKey));
    }

    public ActivityLogKind Kind { get; }
    public string Time { get; }
    public string Message { get; }
    public string SourceKey { get; }
    public string FilterKey { get; }
    public bool IsImportant { get; }
    public string SourceName { get; }
    public string Category { get; }
    public string Title { get; }
    public string Description { get; }
    public Geometry IconGeometry { get; }
    public ImageSource? IconImageSource { get; }
    public Visibility ImageIconVisibility { get; }
    public Visibility OriginalImageIconVisibility { get; }
    public Visibility TintedImageIconVisibility { get; }
    public Visibility VectorIconVisibility { get; }
    public SolidColorBrush AccentBrush { get; }
    public SolidColorBrush IconBackgroundBrush { get; }
    public Geometry SourceIconGeometry { get; }
    public ImageSource? SourceIconImageSource { get; }
    public Visibility SourceImageVisibility { get; }
    public Visibility SourceVectorVisibility { get; }
    public SolidColorBrush SourceBrush { get; }
    public SolidColorBrush SourceBackgroundBrush { get; }
    public string StatusText { get; }
    public ImageSource? StatusIconImageSource { get; }
    public SolidColorBrush StatusBrush { get; }
    public SolidColorBrush StatusBackgroundBrush { get; }

    public bool MatchesFilter(IReadOnlySet<string> enabledFilters, string searchText)
    {
        var sourceEnabled = enabledFilters.Contains(FilterKey);
        var importantEnabled = enabledFilters.Contains("IMPORTANTE");
        var hasAnySourceEnabled = enabledFilters.Any(filter => !string.Equals(filter, "IMPORTANTE", StringComparison.OrdinalIgnoreCase));

        if (IsImportant)
        {
            if (!importantEnabled)
            {
                return false;
            }

            if (hasAnySourceEnabled && !sourceEnabled)
            {
                return false;
            }
        }
        else if (!sourceEnabled)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(searchText))
        {
            return true;
        }

        return Message.Contains(searchText, StringComparison.OrdinalIgnoreCase)
            || Title.Contains(searchText, StringComparison.OrdinalIgnoreCase)
            || Description.Contains(searchText, StringComparison.OrdinalIgnoreCase)
            || SourceName.Contains(searchText, StringComparison.OrdinalIgnoreCase)
            || Category.Contains(searchText, StringComparison.OrdinalIgnoreCase)
            || StatusText.Contains(searchText, StringComparison.OrdinalIgnoreCase);
    }
}
