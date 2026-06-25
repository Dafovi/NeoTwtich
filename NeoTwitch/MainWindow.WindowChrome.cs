using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using Forms = System.Windows.Forms;

namespace NeoTwitch;

public partial class MainWindow
{
    private void ApplyWindowChromeColor()
    {
        try
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            if (hwnd == IntPtr.Zero)
            {
                return;
            }

            var captionColor = AppCaptionColor;
            var borderColor = AppCaptionColor;
            var textColor = AppCaptionTextColor;
            var size = Marshal.SizeOf<int>();
            _ = DwmSetWindowAttribute(hwnd, DwmWindowAttributeCaptionColor, ref captionColor, size);
            _ = DwmSetWindowAttribute(hwnd, DwmWindowAttributeBorderColor, ref borderColor, size);
            _ = DwmSetWindowAttribute(hwnd, DwmWindowAttributeTextColor, ref textColor, size);
        }
        catch
        {
            // Older Windows builds ignore custom title bar colors.
        }
    }

    private void Window_StateChanged(object? sender, EventArgs e)
    {
    }

    private void CustomTitleDragArea_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (IsInsideButton(e.OriginalSource as DependencyObject))
        {
            return;
        }

        if (e.ClickCount >= 2)
        {
            ToggleWindowState();
            return;
        }

        if (_isCustomMaximized)
        {
            RestoreWindowFromWorkArea();
        }

        try
        {
            DragMove();
        }
        catch
        {
            // DragMove can throw if the pointer is released before the drag starts.
        }
    }

    private void MinimizeWindowButton_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void MaximizeWindowButton_Click(object sender, RoutedEventArgs e)
    {
        ToggleWindowState();
    }

    private void CloseWindowButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void ToggleWindowState()
    {
        if (_isCustomMaximized)
        {
            RestoreWindowFromWorkArea();
            return;
        }

        MaximizeWindowToWorkArea();
    }

    private void MaximizeWindowToWorkArea()
    {
        _restoreWindowBounds = new Rect(Left, Top, Width, Height);

        var handle = new WindowInteropHelper(this).Handle;
        var area = Forms.Screen.FromHandle(handle).WorkingArea;
        var source = PresentationSource.FromVisual(this);
        var transform = source?.CompositionTarget?.TransformFromDevice ?? Matrix.Identity;
        var topLeft = transform.Transform(new System.Windows.Point(area.Left, area.Top));
        var bottomRight = transform.Transform(new System.Windows.Point(area.Right, area.Bottom));

        WindowState = WindowState.Normal;
        Left = topLeft.X;
        Top = topLeft.Y;
        Width = Math.Max(MinWidth, bottomRight.X - topLeft.X);
        Height = Math.Max(MinHeight, bottomRight.Y - topLeft.Y);
        _isCustomMaximized = true;
    }

    private void RestoreWindowFromWorkArea()
    {
        WindowState = WindowState.Normal;
        _isCustomMaximized = false;

        if (_restoreWindowBounds.IsEmpty)
        {
            return;
        }

        Left = _restoreWindowBounds.Left;
        Top = _restoreWindowBounds.Top;
        Width = _restoreWindowBounds.Width;
        Height = _restoreWindowBounds.Height;
    }

    private static bool IsInsideButton(DependencyObject? source)
    {
        for (var current = source; current is not null; current = VisualTreeHelper.GetParent(current))
        {
            if (current is System.Windows.Controls.Button)
            {
                return true;
            }
        }

        return false;
    }
}
