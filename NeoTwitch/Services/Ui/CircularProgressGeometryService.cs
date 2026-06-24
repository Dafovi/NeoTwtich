using System.Windows;
using System.Windows.Media;

namespace NeoTwitch.Services.Ui;

public static class CircularProgressGeometryService
{
    public const double DefaultCenter = 52d;
    public const double DefaultRadius = 46d;

    public static int ToPercent(double value, double maximum)
    {
        return maximum <= 0
            ? 0
            : (int)Math.Round(Math.Clamp(value / maximum, 0d, 1d) * 100d);
    }

    public static Geometry BuildArcGeometry(
        double progress,
        double center = DefaultCenter,
        double radius = DefaultRadius)
    {
        progress = Math.Clamp(progress, 0d, 1d);
        if (progress <= 0d)
        {
            return Geometry.Empty;
        }

        var adjustedProgress = progress >= 1d ? 0.9999d : progress;
        var start = PointOnCircle(center, center, radius, -90d);
        var end = PointOnCircle(center, center, radius, -90d + adjustedProgress * 360d);
        var figure = new PathFigure
        {
            StartPoint = start,
            IsClosed = false,
            IsFilled = false
        };

        figure.Segments.Add(new ArcSegment(
            end,
            new System.Windows.Size(radius, radius),
            0,
            adjustedProgress > 0.5d,
            SweepDirection.Clockwise,
            true));

        var geometry = new PathGeometry([figure]);
        geometry.Freeze();
        return geometry;
    }

    public static System.Windows.Point PointOnCircle(double centerX, double centerY, double radius, double degrees)
    {
        var radians = degrees * Math.PI / 180d;
        return new System.Windows.Point(
            centerX + radius * Math.Cos(radians),
            centerY + radius * Math.Sin(radians));
    }
}
