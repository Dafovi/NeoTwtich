namespace NeoTwitch.Services.Ui;

public sealed record ThemePalette(
    System.Windows.Media.SolidColorBrush Window,
    System.Windows.Media.SolidColorBrush Sidebar,
    System.Windows.Media.SolidColorBrush Surface,
    System.Windows.Media.SolidColorBrush Input,
    System.Windows.Media.SolidColorBrush Button,
    System.Windows.Media.SolidColorBrush Border,
    System.Windows.Media.SolidColorBrush Text,
    System.Windows.Media.SolidColorBrush MutedText,
    System.Windows.Media.SolidColorBrush SidebarText,
    System.Windows.Media.SolidColorBrush SidebarMutedText,
    System.Windows.Media.SolidColorBrush SidebarCard,
    System.Windows.Media.SolidColorBrush SidebarCardBorder,
    System.Windows.Media.SolidColorBrush Console,
    System.Windows.Media.SolidColorBrush ConsoleMutedText,
    System.Windows.Media.SolidColorBrush ScrollTrack,
    System.Windows.Media.SolidColorBrush Accent,
    System.Windows.Media.SolidColorBrush NavSelected,
    System.Windows.Media.SolidColorBrush DangerSurface,
    System.Windows.Media.SolidColorBrush DangerText,
    System.Windows.Media.SolidColorBrush DangerBorder)
{
    public static ThemePalette Light { get; } = new(
        BrushFrom("#F7FAFC"),
        BrushFrom("#FFFFFF"),
        BrushFrom("#FFFFFF"),
        BrushFrom("#F8FAFC"),
        BrushFrom("#EEF2F6"),
        BrushFrom("#E2E8F0"),
        BrushFrom("#0B1117"),
        BrushFrom("#475569"),
        BrushFrom("#0B1117"),
        BrushFrom("#64748B"),
        BrushFrom("#F8FAFC"),
        BrushFrom("#E2E8F0"),
        BrushFrom("#0B1117"),
        BrushFrom("#94A3B8"),
        BrushFrom("#E2E8F0"),
        BrushFrom("#14B8A6"),
        BrushFrom("#14B8A6"),
        BrushFrom("#FFF1F2"),
        BrushFrom("#B91C1C"),
        BrushFrom("#FDA4AF"));

    public static ThemePalette Dark { get; } = new(
        BrushFrom("#081117"),
        BrushFrom("#0F1822"),
        BrushFrom("#121A24"),
        BrushFrom("#0F1822"),
        BrushFrom("#162231"),
        BrushFrom("#233142"),
        BrushFrom("#E6EEF2"),
        BrushFrom("#A7B4BE"),
        BrushFrom("#E6EEF2"),
        BrushFrom("#A7B4BE"),
        BrushFrom("#162231"),
        BrushFrom("#233142"),
        BrushFrom("#050A0E"),
        BrushFrom("#64748B"),
        BrushFrom("#132330"),
        BrushFrom("#14B8A6"),
        BrushFrom("#092C2D"),
        BrushFrom("#3A1418"),
        BrushFrom("#FDA4AF"),
        BrushFrom("#7F1D1D"));

    private static System.Windows.Media.SolidColorBrush BrushFrom(string hex)
    {
        var brush = new System.Windows.Media.SolidColorBrush(
            (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(hex));
        brush.Freeze();
        return brush;
    }
}
