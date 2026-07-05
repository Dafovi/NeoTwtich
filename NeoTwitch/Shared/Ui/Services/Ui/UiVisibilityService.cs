using System.Windows;

namespace NeoTwitch.Services.Ui;

public static class UiVisibilityService
{
    public static void SetVisible(bool isVisible, params UIElement[] elements)
    {
        var visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
        foreach (var element in elements)
        {
            element.Visibility = visibility;
        }
    }
}
