using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Habbo_Downloader.Tools;

namespace Habbo_Downloader.App.Professional;

/// <summary>
/// Country flags of the Habbo hotels, drawn as shapes: Windows has no flag emoji, so these look the same everywhere.
/// </summary>
public static class Flags
{
    private const double W = 30, H = 20;
    private const string Earth = "M17.9,17.39C17.64,16.59 16.89,16 16,16H15V13A1,1 0 0,0 14,12H8V10H10A1,1 0 0,0 11,9V7H13A2,2 0 0,0 15,5V4.59C17.93,5.77 20,8.64 20,12C20,14.08 19.2,15.97 17.9,17.39M11,19.93C7.05,19.44 4,16.08 4,12C4,11.38 4.08,10.78 4.21,10.21L9,15V16A2,2 0 0,0 11,18M12,2A10,10 0 0,0 2,12A10,10 0 0,0 12,22A10,10 0 0,0 22,12A10,10 0 0,0 12,2Z";

    /// <summary>The flag of the hotel at the given width (3:2); a globe for Habbo.com and for custom URLs.</summary>
    public static Control For(HabboHotel? hotel, double width = 30)
    {
        var canvas = new Canvas { Width = W, Height = H };
        switch (hotel?.Domain)
        {
            case "habbo.nl": Horizontal(canvas, "#AE1C28", "#FFFFFF", "#21468B"); break;
            case "habbo.de": Horizontal(canvas, "#000000", "#DD0000", "#FFCE00"); break;
            case "habbo.fr": Vertical(canvas, "#0055A4", "#FFFFFF", "#EF4135"); break;
            case "habbo.it": Vertical(canvas, "#009246", "#FFFFFF", "#CE2B37"); break;
            case "habbo.es":
                Rect(canvas, 0, 0, W, H, "#AA151B");
                Rect(canvas, 0, H / 4, W, H / 2, "#F1BF00");
                break;
            case "habbo.fi":
                Rect(canvas, 0, 0, W, H, "#FFFFFF");
                Rect(canvas, W * 5 / 18, 0, W * 3 / 18, H, "#002F6C");
                Rect(canvas, 0, H * 4 / 11, W, H * 3 / 11, "#002F6C");
                break;
            case "habbo.com.tr":
                Rect(canvas, 0, 0, W, H, "#E30A17");
                Circle(canvas, 11, 10, 5, "#FFFFFF");
                Circle(canvas, 12.25, 10, 4, "#E30A17");
                Star(canvas, 16.6, 10, 2.5, "#FFFFFF");
                break;
            case "habbo.com.br":
                Rect(canvas, 0, 0, W, H, "#009C3B");
                canvas.Children.Add(new Polygon
                {
                    Points = [new Point(2.6, 10), new Point(15, 1.7), new Point(27.4, 10), new Point(15, 18.3)],
                    Fill = Solid("#FFDF00")
                });
                Circle(canvas, 15, 10, 5.1, "#002776");
                break;
            default:
                // Habbo.com is international; custom URLs have no country.
                Rect(canvas, 0, 0, W, H, hotel is null ? "#8C97AA" : "#2F6FDE");
                var globe = new Avalonia.Controls.Shapes.Path { Data = Geometry.Parse(Earth), Fill = Brushes.White, Width = 15, Height = 15, Stretch = Stretch.Uniform };
                Canvas.SetLeft(globe, 7.5);
                Canvas.SetTop(globe, 2.5);
                canvas.Children.Add(globe);
                break;
        }

        return new Border
        {
            Width = width,
            Height = width * H / W,
            CornerRadius = new CornerRadius(Math.Max(2, width / 12)),
            ClipToBounds = true,
            BorderThickness = new Thickness(1),
            BorderBrush = Solid("#26000000"),
            Child = new Viewbox { Stretch = Stretch.Fill, Child = canvas }
        };
    }

    private static void Horizontal(Canvas canvas, string top, string middle, string bottom)
    {
        Rect(canvas, 0, 0, W, H / 3, top);
        Rect(canvas, 0, H / 3, W, H / 3, middle);
        Rect(canvas, 0, H * 2 / 3, W, H / 3 + 0.01, bottom);
    }

    private static void Vertical(Canvas canvas, string left, string middle, string right)
    {
        Rect(canvas, 0, 0, W / 3, H, left);
        Rect(canvas, W / 3, 0, W / 3, H, middle);
        Rect(canvas, W * 2 / 3, 0, W / 3 + 0.01, H, right);
    }

    private static void Rect(Canvas canvas, double x, double y, double width, double height, string color)
    {
        var rect = new Rectangle { Width = width, Height = height, Fill = Solid(color) };
        Canvas.SetLeft(rect, x);
        Canvas.SetTop(rect, y);
        canvas.Children.Add(rect);
    }

    private static void Circle(Canvas canvas, double centerX, double centerY, double radius, string color)
    {
        var circle = new Ellipse { Width = radius * 2, Height = radius * 2, Fill = Solid(color) };
        Canvas.SetLeft(circle, centerX - radius);
        Canvas.SetTop(circle, centerY - radius);
        canvas.Children.Add(circle);
    }

    /// <summary>A five-pointed star with one point towards the hoist, as on the Turkish flag.</summary>
    private static void Star(Canvas canvas, double centerX, double centerY, double radius, string color)
    {
        var points = new List<Point>();
        for (int i = 0; i < 10; i++)
        {
            double r = i % 2 == 0 ? radius : radius * 0.4;
            double angle = Math.PI + i * Math.PI / 5;
            points.Add(new Point(centerX + r * Math.Cos(angle), centerY + r * Math.Sin(angle)));
        }
        canvas.Children.Add(new Polygon { Points = points, Fill = Solid(color) });
    }

    private static IBrush Solid(string color) => new SolidColorBrush(Color.Parse(color));
}
