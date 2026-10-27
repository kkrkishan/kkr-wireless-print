using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

namespace AssetGenerator;

class Program
{
    static void Main(string[] args)
    {
        string rootDir = args.Length > 0 ? args[0] : Directory.GetCurrentDirectory();
        string appAssetsDir = Path.Combine(rootDir, "src", "KKR.PrintServer.App", "Assets");
        string installerDir = Path.Combine(rootDir, "installer");

        Directory.CreateDirectory(appAssetsDir);
        Directory.CreateDirectory(installerDir);

        string appIco = Path.Combine(appAssetsDir, "app.ico");
        string installerIco = Path.Combine(installerDir, "app.ico");

        SaveMultiSizeIco(appIco, new[] { 256, 128, 64, 48, 32, 16 });
        File.Copy(appIco, installerIco, true);
        Console.WriteLine($"Generated app.ico at {appIco}");

        string wizLarge = Path.Combine(installerDir, "wizard_large.bmp");
        CreateWizardLarge(wizLarge);
        Console.WriteLine($"Generated wizard_large.bmp at {wizLarge}");

        string wizSmall = Path.Combine(installerDir, "wizard_small.bmp");
        CreateWizardSmall(wizSmall);
        Console.WriteLine($"Generated wizard_small.bmp at {wizSmall}");
    }

    static Bitmap DrawIcon(int size)
    {
        var bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        g.Clear(Color.Transparent);

        float s = size / 256f;

        // Rounded background card (Royal Blue gradient)
        var bgRect = new RectangleF(8 * s, 8 * s, 240 * s, 240 * s);
        using (var brush = new LinearGradientBrush(bgRect, Color.FromArgb(255, 30, 58, 138), Color.FromArgb(255, 14, 116, 144), 45f))
        using (var path = GetRoundedRect(bgRect, 36 * s))
        {
            g.FillPath(brush, path);
            using var borderPen = new Pen(Color.FromArgb(100, 255, 255, 255), Math.Max(1f, 2 * s));
            g.DrawPath(borderPen, path);
        }

        // Wi-Fi arcs
        using (var wavePen = new Pen(Color.FromArgb(255, 56, 189, 248), Math.Max(2f, 6 * s)))
        {
            wavePen.StartCap = LineCap.Round;
            wavePen.EndCap = LineCap.Round;
            g.DrawArc(wavePen, 76 * s, 28 * s, 104 * s, 60 * s, 200, 140);
            g.DrawArc(wavePen, 96 * s, 44 * s, 64 * s, 40 * s, 200, 140);
        }
        using (var dotBrush = new SolidBrush(Color.White))
        {
            g.FillEllipse(dotBrush, 122 * s, 68 * s, 12 * s, 12 * s);
        }

        // Upper Paper Sheet
        var paperRect = new RectangleF(78 * s, 92 * s, 100 * s, 40 * s);
        using (var paperBrush = new SolidBrush(Color.FromArgb(255, 248, 250, 252)))
        {
            g.FillRectangle(paperBrush, paperRect);
        }
        using (var linePen = new Pen(Color.FromArgb(200, 148, 163, 184), Math.Max(1f, 2 * s)))
        {
            g.DrawLine(linePen, 90 * s, 104 * s, 166 * s, 104 * s);
            g.DrawLine(linePen, 90 * s, 116 * s, 148 * s, 116 * s);
        }

        // Printer Body
        var bodyRect = new RectangleF(52 * s, 124 * s, 152 * s, 72 * s);
        using (var bodyPath = GetRoundedRect(bodyRect, 14 * s))
        using (var bodyBrush = new SolidBrush(Color.White))
        using (var bodyPen = new Pen(Color.FromArgb(220, 226, 232, 240), Math.Max(1f, 1.5f * s)))
        {
            g.FillPath(bodyBrush, bodyPath);
            g.DrawPath(bodyPen, bodyPath);
        }

        // Output Slot
        using (var slotBrush = new SolidBrush(Color.FromArgb(255, 30, 41, 59)))
        {
            g.FillRectangle(slotBrush, 68 * s, 152 * s, 120 * s, 16 * s);
        }

        // Printed Output Paper
        var outPaperRect = new RectangleF(78 * s, 160 * s, 100 * s, 46 * s);
        using (var outBrush = new SolidBrush(Color.FromArgb(255, 241, 245, 249)))
        {
            g.FillRectangle(outBrush, outPaperRect);
        }
        using (var outLinePen = new Pen(Color.FromArgb(200, 148, 163, 184), Math.Max(1f, 2 * s)))
        {
            g.DrawLine(outLinePen, 88 * s, 174 * s, 168 * s, 174 * s);
            g.DrawLine(outLinePen, 88 * s, 186 * s, 148 * s, 186 * s);
        }

        // Green Power LED
        using (var ledBrush = new SolidBrush(Color.FromArgb(255, 34, 197, 94)))
        {
            g.FillEllipse(ledBrush, 182 * s, 136 * s, 8 * s, 8 * s);
        }

        return bmp;
    }

    static GraphicsPath GetRoundedRect(RectangleF r, float radius)
    {
        var path = new GraphicsPath();
        float d = radius * 2;
        path.AddArc(r.X, r.Y, d, d, 180, 90);
        path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }

    static void SaveMultiSizeIco(string outPath, int[] sizes)
    {
        var entries = new List<(int size, byte[] bytes)>();
        foreach (int s in sizes)
        {
            using var bmp = DrawIcon(s);
            using var ms = new MemoryStream();
            bmp.Save(ms, ImageFormat.Png);
            entries.Add((s, ms.ToArray()));
        }

        using var fs = File.Create(outPath);
        using var bw = new BinaryWriter(fs);

        bw.Write((ushort)0); // reserved
        bw.Write((ushort)1); // icon type
        bw.Write((ushort)entries.Count); // image count

        int offset = 6 + (entries.Count * 16);

        foreach (var entry in entries)
        {
            byte w = entry.size >= 256 ? (byte)0 : (byte)entry.size;
            byte h = entry.size >= 256 ? (byte)0 : (byte)entry.size;
            bw.Write(w);
            bw.Write(h);
            bw.Write((byte)0); // color count
            bw.Write((byte)0); // reserved
            bw.Write((ushort)1); // planes
            bw.Write((ushort)32); // bpp
            bw.Write((uint)entry.bytes.Length);
            bw.Write((uint)offset);
            offset += entry.bytes.Length;
        }

        foreach (var entry in entries)
        {
            bw.Write(entry.bytes);
        }
    }

    static void CreateWizardLarge(string outPath)
    {
        using var bmp = new Bitmap(164, 314, PixelFormat.Format24bppRgb);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = SmoothingMode.AntiAlias;

        var rect = new Rectangle(0, 0, 164, 314);
        using (var brush = new LinearGradientBrush(rect, Color.FromArgb(255, 15, 23, 42), Color.FromArgb(255, 30, 58, 138), 90f))
        {
            g.FillRectangle(brush, rect);
        }

        using var icon = DrawIcon(80);
        g.DrawImage(icon, 42, 40, 80, 80);

        using var titleFont = new Font("Segoe UI", 11, FontStyle.Bold);
        using var subFont = new Font("Segoe UI", 8.5f, FontStyle.Regular);
        using var textBrush = new SolidBrush(Color.White);
        using var cyanBrush = new SolidBrush(Color.FromArgb(255, 56, 189, 248));
        using var sf = new StringFormat { Alignment = StringAlignment.Center };

        g.DrawString("KKR Wireless Print", titleFont, textBrush, 82, 135, sf);
        g.DrawString("Windows Print Server", subFont, cyanBrush, 82, 160, sf);

        g.DrawString("Print from Android\nand PC over Wi-Fi\nseamlessly.", subFont, textBrush, 82, 210, sf);

        bmp.Save(outPath, ImageFormat.Bmp);
    }

    static void CreateWizardSmall(string outPath)
    {
        using var bmp = new Bitmap(55, 58, PixelFormat.Format24bppRgb);
        using var g = Graphics.FromImage(bmp);
        g.Clear(Color.White);

        using var icon = DrawIcon(48);
        g.DrawImage(icon, 3, 5, 48, 48);

        bmp.Save(outPath, ImageFormat.Bmp);
    }
}
