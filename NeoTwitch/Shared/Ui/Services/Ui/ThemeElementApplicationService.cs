using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using NeoTwitch.ViewModels.Activity;
using static NeoTwitch.Services.Ui.ThemeElementClassifier;
using static NeoTwitch.Services.Ui.UiBrushFactory;
using WpfBrushes = System.Windows.Media.Brushes;
using WpfButton = System.Windows.Controls.Button;
using WpfCheckBox = System.Windows.Controls.CheckBox;
using WpfComboBox = System.Windows.Controls.ComboBox;
using WpfListBox = System.Windows.Controls.ListBox;
using WpfSystemColors = System.Windows.SystemColors;
using WpfTabControl = System.Windows.Controls.TabControl;
using WpfTextBox = System.Windows.Controls.TextBox;

namespace NeoTwitch.Services.Ui;

public static class ThemeElementApplicationService
{
    public static bool TryApply(
        DependencyObject element,
        ThemePalette palette,
        out bool skipChildren)
    {
        skipChildren = false;

        switch (element)
        {
            case Border border:
                ApplyBorderTheme(border, palette);
                return true;
            case TextBlock textBlock:
                ApplyTextBlockTheme(textBlock, palette);
                return true;
            case WpfTextBox textBox:
                ApplyTextBoxTheme(textBox, palette);
                return true;
            case WpfComboBox comboBox:
                ApplyComboBoxTheme(comboBox, palette);
                return true;
            case WpfListBox listBox:
                ApplyListBoxTheme(listBox, palette);
                return true;
            case WpfTabControl tabControl:
                tabControl.Background = WpfBrushes.Transparent;
                tabControl.BorderBrush = palette.Border;
                tabControl.Foreground = palette.Text;
                return true;
            case TabItem tabItem:
                tabItem.Background = palette.Surface;
                tabItem.Foreground = palette.Text;
                tabItem.BorderBrush = palette.Border;
                return true;
            case WpfCheckBox checkBox:
                checkBox.Foreground = palette.Text;
                checkBox.Background = palette.Input;
                checkBox.BorderBrush = palette.MutedText;
                skipChildren = true;
                return true;
            case Slider slider:
                slider.Foreground = palette.Accent;
                return true;
            case WpfButton button when IsColorButton(button):
                button.BorderBrush = palette.Border;
                skipChildren = true;
                return true;
            case ToggleButton toggleButton when RuleStatusFilterButtonThemeService.IsRuleStatusFilterButton(toggleButton):
                RuleStatusFilterButtonThemeService.Apply(toggleButton, palette);
                skipChildren = true;
                return true;
            case ToggleButton toggleButton when ActivityFilterButtonThemeService.IsActivityFilterButton(toggleButton):
                ActivityFilterButtonThemeService.Apply(toggleButton, palette);
                skipChildren = true;
                return true;
            default:
                return false;
        }
    }

    private static void ApplyBorderTheme(Border border, ThemePalette palette)
    {
        if (border.TemplatedParent is not null
            || string.Equals(border.Tag?.ToString(), "StaticBrush", StringComparison.OrdinalIgnoreCase)
            || border.DataContext is ActivityLogEntry)
        {
            return;
        }

        border.BorderBrush = palette.Border;
        if (IsSidebarBorder(border))
        {
            border.Background = palette.Sidebar;
            return;
        }

        if (IsTitleBarBorder(border))
        {
            border.Background = palette.Window;
            border.BorderBrush = palette.Border;
            return;
        }

        if (IsConsoleBorder(border))
        {
            border.Background = palette.Console;
            return;
        }

        if (IsInsideNamedElement(border, "SidebarChrome"))
        {
            border.Background = palette.SidebarCard;
            border.BorderBrush = palette.SidebarCardBorder;
            return;
        }

        border.Background = palette.Surface;
    }

    private static void ApplyTextBlockTheme(TextBlock textBlock, ThemePalette palette)
    {
        if (textBlock.DataContext is ActivityLogEntry
            || string.Equals(textBlock.Tag?.ToString(), "StaticBrush", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (string.Equals(textBlock.Tag?.ToString(), "Accent", StringComparison.OrdinalIgnoreCase))
        {
            textBlock.Foreground = palette.Accent;
            return;
        }

        if (string.Equals(textBlock.Tag?.ToString(), "Success", StringComparison.OrdinalIgnoreCase))
        {
            textBlock.Foreground = FrozenBrushFrom("#22C55E");
            return;
        }

        if (IsInsideNamedElement(textBlock, "SidebarChrome"))
        {
            textBlock.Foreground = textBlock.FontSize <= 12 || textBlock.Name.Contains("Status", StringComparison.OrdinalIgnoreCase)
                ? palette.SidebarMutedText
                : palette.SidebarText;
            return;
        }

        if (IsInsideNamedElement(textBlock, "MiniConsolePanel"))
        {
            textBlock.Foreground = textBlock.FontSize <= 12
                ? palette.ConsoleMutedText
                : WpfBrushes.White;
            return;
        }

        textBlock.Foreground = textBlock.FontSize <= 12 || textBlock.Name.Contains("Status", StringComparison.OrdinalIgnoreCase)
            ? palette.MutedText
            : palette.Text;
    }

    private static void ApplyTextBoxTheme(WpfTextBox textBox, ThemePalette palette)
    {
        textBox.Background = palette.Input;
        textBox.Foreground = palette.Text;
        textBox.BorderBrush = palette.Border;
        textBox.CaretBrush = palette.Text;
    }

    private static void ApplyComboBoxTheme(WpfComboBox comboBox, ThemePalette palette)
    {
        comboBox.Background = palette.Input;
        comboBox.Foreground = palette.Text;
        comboBox.BorderBrush = palette.Border;
        comboBox.Resources[WpfSystemColors.WindowBrushKey] = palette.Input;
        comboBox.Resources[WpfSystemColors.ControlBrushKey] = palette.Input;
        comboBox.Resources[WpfSystemColors.WindowTextBrushKey] = palette.Text;
        comboBox.Resources[WpfSystemColors.ControlTextBrushKey] = palette.Text;
        comboBox.Resources[WpfSystemColors.HighlightBrushKey] = palette.Accent;
        comboBox.Resources[WpfSystemColors.HighlightTextBrushKey] = WpfBrushes.White;
    }

    private static void ApplyListBoxTheme(WpfListBox listBox, ThemePalette palette)
    {
        if (IsInsideNamedElement(listBox, "MiniConsolePanel"))
        {
            listBox.Background = WpfBrushes.Transparent;
            listBox.Foreground = WpfBrushes.White;
            listBox.BorderBrush = WpfBrushes.Transparent;
            return;
        }

        if (IsActivityFeedListBox(listBox))
        {
            listBox.Background = WpfBrushes.Transparent;
            listBox.Foreground = palette.Text;
            listBox.BorderBrush = WpfBrushes.Transparent;
            return;
        }

        listBox.Background = palette.Input;
        listBox.Foreground = palette.Text;
        listBox.BorderBrush = palette.Border;
    }
}
