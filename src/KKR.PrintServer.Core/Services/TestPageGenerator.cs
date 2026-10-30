using System.Drawing;
using System.Drawing.Drawing2D;
using KKR.PrintServer.Core.Models;

namespace KKR.PrintServer.Core.Services;

public static class TestPageGenerator
{
    public static Bitmap GenerateTestPageImage(string printerName, PrinterInfo? info, string serverIp, int port, bool httpsEnabled)
    {
        // Standard Letter/A4 canvas at 300 DPI (approx 2480 x 3508 pixels)
        int width = 2480;
        int height = 3508;
        var bitmap = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        bitmap.SetResolution(300, 300);

        using var g = Graphics.FromImage(bitmap);
        g.Clear(Color.White);
        g.SmoothingMode = SmoothingMode.HighQuality;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;

        // Border margin test (1/2 inch at 300 dpi = 150 px)
        using var marginPen = new Pen(Color.FromArgb(180, 180, 180), 3) { DashStyle = DashStyle.Dash };
        g.DrawRectangle(marginPen, 150, 150, width - 300, height - 300);

        // Header Banner
        using var headerBrush = new SolidBrush(Color.FromArgb(24, 76, 120));
        g.FillRectangle(headerBrush, 150, 150, width - 300, 220);

        using var titleFont = new Font("Arial", 42, FontStyle.Bold);
        using var whiteBrush = new SolidBrush(Color.White);
        g.DrawString("PRINT OFO", titleFont, whiteBrush, 200, 190);

        using var subFont = new Font("Arial", 20, FontStyle.Regular);
        using var yellowBrush = new SolidBrush(Color.FromArgb(255, 215, 0));
        g.DrawString("Windows Print Server — Diagnostic Test Page", subFont, yellowBrush, 205, 270);

        // Metadata Box
        int y = 420;
        using var labelFont = new Font("Arial", 22, FontStyle.Bold);
        using var valueFont = new Font("Arial", 22, FontStyle.Regular);
        using var textBrush = new SolidBrush(Color.FromArgb(30, 30, 30));

        void DrawField(string label, string value)
        {
            g.DrawString(label, labelFont, textBrush, 200, y);
            g.DrawString(value, valueFont, textBrush, 700, y);
            y += 65;
        }

        DrawField("Target Printer:", printerName);
        DrawField("Driver:", info?.DriverName ?? "Windows Print Driver");
        DrawField("Port:", info?.PortName ?? "USB / Local");
        DrawField("Color Supported:", (info?.Capabilities.SupportsColor ?? false) ? "Yes" : "No (Monochrome)");
        DrawField("Duplex Supported:", (info?.Capabilities.CanDuplex ?? false) ? "Yes" : "No");
        DrawField("Server Endpoint:", $"{(httpsEnabled ? "https" : "http")}://{serverIp}:{port}");
        DrawField("Timestamp:", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss dddd"));

        y += 40;
        using var divPen = new Pen(Color.FromArgb(200, 200, 200), 2);
        g.DrawLine(divPen, 200, y, width - 200, y);
        y += 50;

        // Color & Grayscale Calibration Bars
        g.DrawString("Color and Grayscale Calibration", labelFont, textBrush, 200, y);
        y += 60;

        int barWidth = (width - 400) / 6;
        int barHeight = 80;
        Color[] testColors = {
            Color.FromArgb(0, 164, 228),   // Cyan
            Color.FromArgb(236, 0, 140),   // Magenta
            Color.FromArgb(255, 242, 0),   // Yellow
            Color.FromArgb(35, 31, 32),    // Black
            Color.FromArgb(0, 150, 64),    // Green
            Color.FromArgb(220, 40, 40)    // Red
        };

        for (int i = 0; i < testColors.Length; i++)
        {
            using var b = new SolidBrush(testColors[i]);
            g.FillRectangle(b, 200 + (i * barWidth), y, barWidth - 10, barHeight);
            using var border = new Pen(Color.FromArgb(80, 80, 80), 1);
            g.DrawRectangle(border, 200 + (i * barWidth), y, barWidth - 10, barHeight);
        }

        y += barHeight + 30;

        // Grayscale 10-step Gradient
        int graySteps = 10;
        int stepWidth = (width - 400) / graySteps;
        for (int i = 0; i < graySteps; i++)
        {
            int grayVal = (int)(255 * (i / (float)(graySteps - 1)));
            using var b = new SolidBrush(Color.FromArgb(grayVal, grayVal, grayVal));
            g.FillRectangle(b, 200 + (i * stepWidth), y, stepWidth - 4, 60);
            using var border = new Pen(Color.FromArgb(120, 120, 120), 1);
            g.DrawRectangle(border, 200 + (i * stepWidth), y, stepWidth - 4, 60);
        }

        y += 120;
        g.DrawLine(divPen, 200, y, width - 200, y);
        y += 50;

        // Fine Line Sharpness & Alignment Test
        g.DrawString("Resolution and Alignment Pattern", labelFont, textBrush, 200, y);
        y += 70;

        int centerX = width / 2;
        int patternCenterY = y + 250;
        using var finePen = new Pen(Color.Black, 1);
        for (int angle = 0; angle < 360; angle += 15)
        {
            double rad = angle * Math.PI / 180.0;
            float endX = centerX + (float)(Math.Cos(rad) * 220);
            float endY = patternCenterY + (float)(Math.Sin(rad) * 220);
            g.DrawLine(finePen, centerX, patternCenterY, endX, endY);
        }

        // Concentric circles
        for (int r = 40; r <= 220; r += 30)
        {
            g.DrawEllipse(finePen, centerX - r, patternCenterY - r, r * 2, r * 2);
        }

        // Footer note
        using var footerFont = new Font("Arial", 16, FontStyle.Italic);
        using var footerBrush = new SolidBrush(Color.FromArgb(100, 100, 100));
        string footerText = "Print Ofo — Verified Windows Spooler Pipeline & Hardware Driver Communication";
        var footerSize = g.MeasureString(footerText, footerFont);
        g.DrawString(footerText, footerFont, footerBrush, (width - footerSize.Width) / 2, height - 220);

        return bitmap;
    }
}
