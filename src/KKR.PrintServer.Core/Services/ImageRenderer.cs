using System.Drawing;
using System.Drawing.Drawing2D;
using KKR.PrintServer.Core.Models;

namespace KKR.PrintServer.Core.Services;

public static class ImageRenderer
{
    private const int ExifOrientationId = 0x0112;

    public static Bitmap LoadImageWithExifOrientation(string filePath)
    {
        using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        var original = new Bitmap(stream);

        // Normalize EXIF orientation
        if (original.PropertyIdList.Contains(ExifOrientationId))
        {
            var prop = original.GetPropertyItem(ExifOrientationId);
            if (prop?.Value != null && prop.Value.Length > 0)
            {
                int val = BitConverter.ToUInt16(prop.Value, 0);
                var rotateFlip = val switch
                {
                    1 => RotateFlipType.RotateNoneFlipNone,
                    2 => RotateFlipType.RotateNoneFlipX,
                    3 => RotateFlipType.Rotate180FlipNone,
                    4 => RotateFlipType.Rotate180FlipX,
                    5 => RotateFlipType.Rotate90FlipX,
                    6 => RotateFlipType.Rotate90FlipNone,
                    7 => RotateFlipType.Rotate270FlipX,
                    8 => RotateFlipType.Rotate270FlipNone,
                    _ => RotateFlipType.RotateNoneFlipNone
                };

                if (rotateFlip != RotateFlipType.RotateNoneFlipNone)
                {
                    original.RotateFlip(rotateFlip);
                }
            }
        }

        // Return a copy so stream is completely released
        var copy = new Bitmap(original.Width, original.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        copy.SetResolution(original.HorizontalResolution > 0 ? original.HorizontalResolution : 96,
                           original.VerticalResolution > 0 ? original.VerticalResolution : 96);

        using var g = Graphics.FromImage(copy);
        g.Clear(Color.White);
        g.DrawImage(original, 0, 0, original.Width, original.Height);

        return copy;
    }

    public static void DrawImageToPrinter(Graphics g, Rectangle bounds, Image image, PrintScalingMode scalingMode, bool centerOnPage)
    {
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.SmoothingMode = SmoothingMode.HighQuality;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        g.CompositingQuality = CompositingQuality.HighQuality;

        float destX = bounds.Left;
        float destY = bounds.Top;
        float destWidth = bounds.Width;
        float destHeight = bounds.Height;

        float imgWidth = image.Width;
        float imgHeight = image.Height;

        // Image aspect ratio
        float imgAspect = imgWidth / imgHeight;
        float pageAspect = (float)bounds.Width / bounds.Height;

        switch (scalingMode)
        {
            case PrintScalingMode.ActualSize:
                // 100% scale based on image DPI or 1/100th inch coordinates
                // In PrintDocument, bounds are in hundredths of an inch (1/100 inch)
                // If image has resolution DPI:
                float dpiX = image.HorizontalResolution > 0 ? image.HorizontalResolution : 96f;
                float dpiY = image.VerticalResolution > 0 ? image.VerticalResolution : 96f;
                destWidth = (imgWidth / dpiX) * 100f;
                destHeight = (imgHeight / dpiY) * 100f;

                if (centerOnPage)
                {
                    destX = bounds.Left + (bounds.Width - destWidth) / 2f;
                    destY = bounds.Top + (bounds.Height - destHeight) / 2f;
                }
                break;

            case PrintScalingMode.FillPage:
                // Fill entire page, crop overhang, preserve aspect ratio
                if (imgAspect > pageAspect)
                {
                    destHeight = bounds.Height;
                    destWidth = bounds.Height * imgAspect;
                    destY = bounds.Top;
                    destX = bounds.Left + (bounds.Width - destWidth) / 2f;
                }
                else
                {
                    destWidth = bounds.Width;
                    destHeight = bounds.Width / imgAspect;
                    destX = bounds.Left;
                    destY = bounds.Top + (bounds.Height - destHeight) / 2f;
                }
                break;

            case PrintScalingMode.FitPage:
            default:
                // Fit within bounds without cropping, preserving aspect ratio
                if (imgAspect > pageAspect)
                {
                    destWidth = bounds.Width;
                    destHeight = bounds.Width / imgAspect;
                }
                else
                {
                    destHeight = bounds.Height;
                    destWidth = bounds.Height * imgAspect;
                }

                if (centerOnPage)
                {
                    destX = bounds.Left + (bounds.Width - destWidth) / 2f;
                    destY = bounds.Top + (bounds.Height - destHeight) / 2f;
                }
                else
                {
                    destX = bounds.Left;
                    destY = bounds.Top;
                }
                break;
        }

        var destRect = new RectangleF(destX, destY, destWidth, destHeight);
        g.DrawImage(image, destRect);
    }
}
