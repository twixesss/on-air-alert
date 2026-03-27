using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace OnAirAlert.Services;

public static class IconGenerator
{
    private const int Size = 32;
    private const int Center = Size / 2;
    private const int Radius = Center - 2;

    public static WindowIcon CreateNumberIcon(int number, bool isLive = false)
    {
        var bitmap = new RenderTargetBitmap(new PixelSize(Size, Size), new Vector(96, 96));

        using (var ctx = bitmap.CreateDrawingContext())
        {
            // Background circle
            var bgColor = isLive ? Color.FromRgb(204, 0, 0) : Color.FromRgb(30, 144, 255);
            var bgBrush = new SolidColorBrush(bgColor);
            var centerPoint = new Point(Center, Center);
            ctx.DrawEllipse(bgBrush, null, centerPoint, Radius, Radius);

            // Number text
            var text = number > 99 ? "99" : number.ToString();
            var fontSize = text.Length > 1 ? 14 : 18;
            var typeface = new Typeface("Segoe UI", FontStyle.Normal, FontWeight.Bold);
            var formattedText = new FormattedText(
                text,
                System.Globalization.CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                typeface,
                fontSize,
                Brushes.White);

            var textX = Center - formattedText.Width / 2;
            var textY = Center - formattedText.Height / 2;
            ctx.DrawText(formattedText, new Point(textX, textY));
        }

        using var ms = new MemoryStream();
        bitmap.Save(ms);
        ms.Seek(0, SeekOrigin.Begin);
        return new WindowIcon(ms);
    }

    public static WindowIcon CreateLiveIcon()
    {
        var bitmap = new RenderTargetBitmap(new PixelSize(Size, Size), new Vector(96, 96));

        using (var ctx = bitmap.CreateDrawingContext())
        {
            // Red circle
            var bgBrush = new SolidColorBrush(Color.FromRgb(204, 0, 0));
            ctx.DrawEllipse(bgBrush, null, new Point(Center, Center), Radius, Radius);

            // "!" text
            var typeface = new Typeface("Segoe UI", FontStyle.Normal, FontWeight.Bold);
            var formattedText = new FormattedText(
                "!",
                System.Globalization.CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                typeface,
                20,
                Brushes.White);

            var textX = Center - formattedText.Width / 2;
            var textY = Center - formattedText.Height / 2;
            ctx.DrawText(formattedText, new Point(textX, textY));
        }

        using var ms = new MemoryStream();
        bitmap.Save(ms);
        ms.Seek(0, SeekOrigin.Begin);
        return new WindowIcon(ms);
    }

    public static WindowIcon CreateDefaultIcon()
    {
        // Try file first
        var iconPath = System.IO.Path.Combine(System.AppContext.BaseDirectory, "Resources", "icon.ico");
        if (File.Exists(iconPath))
            return new WindowIcon(iconPath);

        // Blue circle
        var bitmap = new WriteableBitmap(
            new PixelSize(Size, Size),
            new Vector(96, 96),
            Avalonia.Platform.PixelFormat.Bgra8888,
            AlphaFormat.Premul);

        using (var fb = bitmap.Lock())
        {
            unsafe
            {
                var ptr = (byte*)fb.Address;
                for (int y = 0; y < Size; y++)
                {
                    for (int x = 0; x < Size; x++)
                    {
                        int offset = y * fb.RowBytes + x * 4;
                        double dx = x - Center, dy = y - Center;
                        if (dx * dx + dy * dy <= Radius * Radius)
                        {
                            ptr[offset + 0] = 0xFF; // B
                            ptr[offset + 1] = 0x90; // G
                            ptr[offset + 2] = 0x1E; // R
                            ptr[offset + 3] = 0xFF; // A
                        }
                    }
                }
            }
        }

        using var ms = new MemoryStream();
        bitmap.Save(ms);
        ms.Seek(0, SeekOrigin.Begin);
        return new WindowIcon(ms);
    }
}
