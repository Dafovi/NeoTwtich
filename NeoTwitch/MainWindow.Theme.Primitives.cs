using System.Windows;
using System.Windows.Controls;
using NeoTwitch.Services.Ui;
using static NeoTwitch.Services.Ui.ThemeElementClassifier;
using static NeoTwitch.Services.Ui.UiBrushFactory;

namespace NeoTwitch;

public partial class MainWindow
{
    private void UpdateCloseBehaviorCards()
    {
        if (_initializingComponent)
        {
            return;
        }

        var closeToTray = CloseToTrayCheck.IsChecked == true;
        if (CloseToTrayRadio.IsChecked != closeToTray)
        {
            CloseToTrayRadio.IsChecked = closeToTray;
        }

        if (CloseAppRadio.IsChecked != !closeToTray)
        {
            CloseAppRadio.IsChecked = !closeToTray;
        }

        var palette = _config.DarkMode
            ? ThemePalette.Dark
            : ThemePalette.Light;
        ApplyCloseBehaviorCardTheme(CloseToTrayCard, closeToTray, palette);
        ApplyCloseBehaviorCardTheme(CloseAppCard, !closeToTray, palette);
    }

    private static void ApplyCloseBehaviorCardTheme(Border card, bool selected, ThemePalette palette)
    {
        card.Background = selected
            ? TranslucentBrushFrom("#14B8A6")
            : palette.Input;
        card.BorderBrush = selected
            ? palette.Accent
            : palette.Border;
    }

    private void ApplyButtonTheme(System.Windows.Controls.Button button, ThemePalette palette)
    {
        if (TryFindResource("NavButton") is Style navButtonStyle &&
            ReferenceEquals(button.Style, navButtonStyle))
        {
            ApplyNavigationButtonTheme(button, palette);
            return;
        }

        if (IsWindowControlButton(button))
        {
            button.Background = System.Windows.Media.Brushes.Transparent;
            button.Foreground = palette.MutedText;
            button.BorderBrush = System.Windows.Media.Brushes.Transparent;
            return;
        }

        if (TryFindResource("PrimaryButton") is Style primaryButtonStyle &&
            ReferenceEquals(button.Style, primaryButtonStyle))
        {
            button.Background = palette.Accent;
            button.Foreground = System.Windows.Media.Brushes.White;
            button.BorderBrush = palette.Accent;
            return;
        }

        if (TryFindResource("DangerButton") is Style dangerButtonStyle &&
            ReferenceEquals(button.Style, dangerButtonStyle))
        {
            button.Background = palette.DangerSurface;
            button.Foreground = palette.DangerText;
            button.BorderBrush = palette.DangerBorder;
            return;
        }

        button.Background = palette.Button;
        button.Foreground = palette.Text;
        button.BorderBrush = palette.Border;
    }

}
