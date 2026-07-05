using System.Windows;
using System.Windows.Media;

namespace NeoTwitch.Services.Ui;

public static class VisualTreeTraversalService
{
    public static IEnumerable<T> FindChildren<T>(DependencyObject root)
        where T : DependencyObject
    {
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
        {
            var child = VisualTreeHelper.GetChild(root, index);
            if (child is T typedChild)
            {
                yield return typedChild;
            }

            foreach (var descendant in FindChildren<T>(child))
            {
                yield return descendant;
            }
        }
    }
}
