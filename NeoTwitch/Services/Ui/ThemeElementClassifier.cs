using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WpfButton = System.Windows.Controls.Button;
using WpfListBox = System.Windows.Controls.ListBox;

namespace NeoTwitch.Services.Ui;

public static class ThemeElementClassifier
{
    public static bool IsColorButton(WpfButton button)
    {
        return !string.IsNullOrWhiteSpace(button.Name)
            && button.Name.EndsWith("ColorButton", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsActivityFeedListBox(WpfListBox listBox)
    {
        return string.Equals(listBox.Name, "ActivityList", StringComparison.OrdinalIgnoreCase)
            || string.Equals(listBox.Name, "DashboardActivityList", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsWindowControlButton(WpfButton button)
    {
        return string.Equals(button.Name, "MinimizeWindowButton", StringComparison.OrdinalIgnoreCase)
            || string.Equals(button.Name, "MaximizeWindowButton", StringComparison.OrdinalIgnoreCase)
            || string.Equals(button.Name, "CloseWindowButton", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsSidebarBorder(Border border)
    {
        return string.Equals(border.Name, "SidebarChrome", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsTitleBarBorder(Border border)
    {
        return string.Equals(border.Name, "TitleBarChrome", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsConsoleBorder(Border border)
    {
        return string.Equals(border.Name, "MiniConsolePanel", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsInsideNamedElement(DependencyObject element, string name)
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
