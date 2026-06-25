using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using NeoTwitch.Models;
using NeoTwitch.Services;
using NeoTwitch.Services.Ui;
using NeoTwitch.ViewModels.Activity;
using NeoTwitch.ViewModels.Library;
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

    private void ApplyTheme()
    {
        _config.DarkMode = ThemeModeService.ResolveDarkMode(_config.ThemeMode);
        var palette = _config.DarkMode
            ? ThemePalette.Dark
            : ThemePalette.Light;

        Background = palette.Window;
        Resources["ThemeWindowBrush"] = palette.Window;
        Resources["ThemeSidebarBrush"] = palette.Sidebar;
        Resources["ThemeSurfaceBrush"] = palette.Surface;
        Resources["ThemeButtonBrush"] = palette.Button;
        Resources["ThemeTextBrush"] = palette.Text;
        Resources["ThemeMutedTextBrush"] = palette.MutedText;
        Resources["ThemeSidebarTextBrush"] = palette.SidebarText;
        Resources["ThemeSidebarMutedTextBrush"] = palette.SidebarMutedText;
        Resources["ThemeInputBrush"] = palette.Input;
        Resources["ThemeBorderBrush"] = palette.Border;
        Resources["ThemeSelectionBrush"] = palette.Accent;
        Resources["ThemeConsoleBrush"] = palette.Console;
        Resources["ThemeScrollThumbBrush"] = palette.Accent;
        Resources["ThemeScrollTrackBrush"] = palette.ScrollTrack;
        Resources[System.Windows.SystemColors.WindowBrushKey] = palette.Input;
        Resources[System.Windows.SystemColors.ControlBrushKey] = palette.Input;
        Resources[System.Windows.SystemColors.WindowTextBrushKey] = palette.Text;
        Resources[System.Windows.SystemColors.ControlTextBrushKey] = palette.Text;
        Resources[System.Windows.SystemColors.HighlightBrushKey] = palette.Accent;
        Resources[System.Windows.SystemColors.HighlightTextBrushKey] = System.Windows.Media.Brushes.White;
        Resources[System.Windows.SystemColors.InactiveSelectionHighlightBrushKey] = palette.Accent;
        Resources[System.Windows.SystemColors.InactiveSelectionHighlightTextBrushKey] = System.Windows.Media.Brushes.White;
        ApplyWindowChromeColor();
        UpdateNavigationButtons();
        ApplyBackgroundOutputMode();
        ApplyThemeToElement(this, palette);
        ApplyBackgroundOutputMode();
        UpdateTwitchLiveIndicator();
        UpdateDashboardSummary();
        UpdateColorButtons();
        UpdateEventKindTileSelection();
        UpdatePatternTileSelection();
        UpdateBackgroundPatternTileSelection();
        UpdateRuleAudioModeSelection();
        UpdateRuleObsMediaModeSelection();
        UpdateAudioFilterButtons();
        UpdateMediaFilterButtons(MediaLibraryKind.Image);
        UpdateMediaFilterButtons(MediaLibraryKind.Video);
        UpdateCloseBehaviorCards();
    }

    private void ApplyThemeToElement(DependencyObject element, ThemePalette palette)
    {
        var skipChildren = false;

        switch (element)
        {
            case Border border when border.TemplatedParent is not null:
                break;
            case Border border when string.Equals(border.Tag?.ToString(), "StaticBrush", StringComparison.OrdinalIgnoreCase):
                break;
            case Border border when border.DataContext is ActivityLogEntry:
                break;
            case Border border:
                border.BorderBrush = palette.Border;
                if (IsSidebarBorder(border))
                {
                    border.Background = palette.Sidebar;
                    break;
                }

                if (IsTitleBarBorder(border))
                {
                    border.Background = palette.Window;
                    border.BorderBrush = palette.Border;
                    break;
                }

                if (IsConsoleBorder(border))
                {
                    border.Background = palette.Console;
                    break;
                }

                if (IsInsideNamedElement(border, "SidebarChrome"))
                {
                    border.Background = palette.SidebarCard;
                    border.BorderBrush = palette.SidebarCardBorder;
                    break;
                }

                border.Background = palette.Surface;
                break;
            case TextBlock textBlock when textBlock.DataContext is ActivityLogEntry:
                break;
            case TextBlock textBlock when string.Equals(textBlock.Tag?.ToString(), "StaticBrush", StringComparison.OrdinalIgnoreCase):
                break;
            case TextBlock textBlock when string.Equals(textBlock.Tag?.ToString(), "Accent", StringComparison.OrdinalIgnoreCase):
                textBlock.Foreground = palette.Accent;
                break;
            case TextBlock textBlock when string.Equals(textBlock.Tag?.ToString(), "Success", StringComparison.OrdinalIgnoreCase):
                textBlock.Foreground = FrozenBrushFrom("#22C55E");
                break;
            case TextBlock textBlock:
                if (IsInsideNamedElement(textBlock, "SidebarChrome"))
                {
                    textBlock.Foreground = textBlock.FontSize <= 12 || textBlock.Name.Contains("Status", StringComparison.OrdinalIgnoreCase)
                        ? palette.SidebarMutedText
                        : palette.SidebarText;
                    break;
                }

                if (IsInsideNamedElement(textBlock, "MiniConsolePanel"))
                {
                    textBlock.Foreground = textBlock.FontSize <= 12
                        ? palette.ConsoleMutedText
                        : System.Windows.Media.Brushes.White;
                    break;
                }

                textBlock.Foreground = textBlock.FontSize <= 12 || textBlock.Name.Contains("Status", StringComparison.OrdinalIgnoreCase)
                    ? palette.MutedText
                    : palette.Text;
                break;
            case System.Windows.Controls.TextBox textBox:
                textBox.Background = palette.Input;
                textBox.Foreground = palette.Text;
                textBox.BorderBrush = palette.Border;
                textBox.CaretBrush = palette.Text;
                break;
            case System.Windows.Controls.ComboBox comboBox:
                comboBox.Background = palette.Input;
                comboBox.Foreground = palette.Text;
                comboBox.BorderBrush = palette.Border;
                comboBox.Resources[System.Windows.SystemColors.WindowBrushKey] = palette.Input;
                comboBox.Resources[System.Windows.SystemColors.ControlBrushKey] = palette.Input;
                comboBox.Resources[System.Windows.SystemColors.WindowTextBrushKey] = palette.Text;
                comboBox.Resources[System.Windows.SystemColors.ControlTextBrushKey] = palette.Text;
                comboBox.Resources[System.Windows.SystemColors.HighlightBrushKey] = palette.Accent;
                comboBox.Resources[System.Windows.SystemColors.HighlightTextBrushKey] = System.Windows.Media.Brushes.White;
                break;
            case System.Windows.Controls.ListBox listBox:
                if (IsInsideNamedElement(listBox, "MiniConsolePanel"))
                {
                    listBox.Background = System.Windows.Media.Brushes.Transparent;
                    listBox.Foreground = System.Windows.Media.Brushes.White;
                    listBox.BorderBrush = System.Windows.Media.Brushes.Transparent;
                    break;
                }

                if (IsActivityFeedListBox(listBox))
                {
                    listBox.Background = System.Windows.Media.Brushes.Transparent;
                    listBox.Foreground = palette.Text;
                    listBox.BorderBrush = System.Windows.Media.Brushes.Transparent;
                    break;
                }

                listBox.Background = palette.Input;
                listBox.Foreground = palette.Text;
                listBox.BorderBrush = palette.Border;
                break;
            case System.Windows.Controls.TabControl tabControl:
                tabControl.Background = System.Windows.Media.Brushes.Transparent;
                tabControl.BorderBrush = palette.Border;
                tabControl.Foreground = palette.Text;
                break;
            case TabItem tabItem:
                tabItem.Background = palette.Surface;
                tabItem.Foreground = palette.Text;
                tabItem.BorderBrush = palette.Border;
                break;
            case System.Windows.Controls.CheckBox checkBox:
                checkBox.Foreground = palette.Text;
                checkBox.Background = palette.Input;
                checkBox.BorderBrush = palette.MutedText;
                skipChildren = true;
                break;
            case Slider slider:
                slider.Foreground = palette.Accent;
                break;
            case System.Windows.Controls.Button button when IsColorButton(button):
                button.BorderBrush = palette.Border;
                skipChildren = true;
                break;
            case ToggleButton toggleButton when IsRuleStatusFilterButton(toggleButton):
                ApplyRuleStatusFilterButtonTheme(toggleButton, palette);
                skipChildren = true;
                break;
            case ToggleButton toggleButton:
                ApplyActivityFilterButtonTheme(toggleButton, palette);
                skipChildren = true;
                break;
            case System.Windows.Controls.Button button:
                ApplyButtonTheme(button, palette);
                skipChildren = true;
                break;
        }

        if (skipChildren)
        {
            return;
        }

        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(element); i++)
        {
            ApplyThemeToElement(VisualTreeHelper.GetChild(element, i), palette);
        }
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

    private static bool IsColorButton(System.Windows.Controls.Button button)
    {
        return !string.IsNullOrWhiteSpace(button.Name)
            && button.Name.EndsWith("ColorButton", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsActivityFeedListBox(System.Windows.Controls.ListBox listBox)
    {
        return string.Equals(listBox.Name, "ActivityList", StringComparison.OrdinalIgnoreCase)
            || string.Equals(listBox.Name, "DashboardActivityList", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsWindowControlButton(System.Windows.Controls.Button button)
    {
        return string.Equals(button.Name, "MinimizeWindowButton", StringComparison.OrdinalIgnoreCase)
            || string.Equals(button.Name, "MaximizeWindowButton", StringComparison.OrdinalIgnoreCase)
            || string.Equals(button.Name, "CloseWindowButton", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSidebarBorder(Border border)
    {
        return string.Equals(border.Name, "SidebarChrome", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsTitleBarBorder(Border border)
    {
        return string.Equals(border.Name, "TitleBarChrome", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsConsoleBorder(Border border)
    {
        return string.Equals(border.Name, "MiniConsolePanel", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsInsideNamedElement(DependencyObject element, string name)
    {
        for (var current = element; current is not null; current = VisualTreeHelper.GetParent(current))
        {
            if (current is FrameworkElement frameworkElement
                && string.Equals(frameworkElement.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
