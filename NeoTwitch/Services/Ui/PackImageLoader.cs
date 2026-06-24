using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace NeoTwitch.Services.Ui;

public static class PackImageLoader
{
    public static ImageSource? Load(string path)
    {
        foreach (var uri in new[]
        {
            $"pack://application:,,,/NeoTwitch;component/{path}",
            $"pack://application:,,,/{path}"
        })
        {
            try
            {
                var image = new BitmapImage(new Uri(uri, UriKind.Absolute));
                image.Freeze();
                return image;
            }
            catch
            {
                // Some WPF resource contexts prefer the assembly-qualified URI, others the app-root URI.
            }
        }

        return null;
    }
}
