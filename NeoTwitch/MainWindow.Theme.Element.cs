using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using NeoTwitch.Services.Ui;
using NeoTwitch.ViewModels.Activity;
using static NeoTwitch.Services.Ui.UiBrushFactory;
using WpfBrushes = System.Windows.Media.Brushes;
using WpfButton = System.Windows.Controls.Button;
using WpfCheckBox = System.Windows.Controls.CheckBox;
using WpfComboBox = System.Windows.Controls.ComboBox;
using WpfListBox = System.Windows.Controls.ListBox;
using WpfSystemColors = System.Windows.SystemColors;
using WpfTabControl = System.Windows.Controls.TabControl;
using WpfTextBox = System.Windows.Controls.TextBox;

namespace NeoTwitch;

public partial class MainWindow
{
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
                        : WpfBrushes.White;
                    break;
                }

                textBlock.Foreground = textBlock.FontSize <= 12 || textBlock.Name.Contains("Status", StringComparison.OrdinalIgnoreCase)
                    ? palette.MutedText
                    : palette.Text;
                break;
            case WpfTextBox textBox:
                textBox.Background = palette.Input;
                textBox.Foreground = palette.Text;
                textBox.BorderBrush = palette.Border;
                textBox.CaretBrush = palette.Text;
                break;
            case WpfComboBox comboBox:
                comboBox.Background = palette.Input;
                comboBox.Foreground = palette.Text;
                comboBox.BorderBrush = palette.Border;
                comboBox.Resources[WpfSystemColors.WindowBrushKey] = palette.Input;
                comboBox.Resources[WpfSystemColors.ControlBrushKey] = palette.Input;
                comboBox.Resources[WpfSystemColors.WindowTextBrushKey] = palette.Text;
                comboBox.Resources[WpfSystemColors.ControlTextBrushKey] = palette.Text;
                comboBox.Resources[WpfSystemColors.HighlightBrushKey] = palette.Accent;
                comboBox.Resources[WpfSystemColors.HighlightTextBrushKey] = WpfBrushes.White;
                break;
            case WpfListBox listBox:
                if (IsInsideNamedElement(listBox, "MiniConsolePanel"))
                {
                    listBox.Background = WpfBrushes.Transparent;
                    listBox.Foreground = WpfBrushes.White;
                    listBox.BorderBrush = WpfBrushes.Transparent;
                    break;
                }

                if (IsActivityFeedListBox(listBox))
                {
                    listBox.Background = WpfBrushes.Transparent;
                    listBox.Foreground = palette.Text;
                    listBox.BorderBrush = WpfBrushes.Transparent;
                    break;
                }

                listBox.Background = palette.Input;
                listBox.Foreground = palette.Text;
                listBox.BorderBrush = palette.Border;
                break;
            case WpfTabControl tabControl:
                tabControl.Background = WpfBrushes.Transparent;
                tabControl.BorderBrush = palette.Border;
                tabControl.Foreground = palette.Text;
                break;
            case TabItem tabItem:
                tabItem.Background = palette.Surface;
                tabItem.Foreground = palette.Text;
                tabItem.BorderBrush = palette.Border;
                break;
            case WpfCheckBox checkBox:
                checkBox.Foreground = palette.Text;
                checkBox.Background = palette.Input;
                checkBox.BorderBrush = palette.MutedText;
                skipChildren = true;
                break;
            case Slider slider:
                slider.Foreground = palette.Accent;
                break;
            case WpfButton button when IsColorButton(button):
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
            case WpfButton button:
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
}
