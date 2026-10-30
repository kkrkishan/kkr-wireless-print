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
        string msStoreDir = Path.Combine(rootDir, "store_assets");
        string playStoreDir = Path.Combine(rootDir, "release", "playstore_assets");
        string androidResDir = Path.Combine(rootDir, "android_client", "android", "app", "src", "main", "res");

        Directory.CreateDirectory(appAssetsDir);
        Directory.CreateDirectory(installerDir);
        Directory.CreateDirectory(msStoreDir);
        Directory.CreateDirectory(playStoreDir);

        Console.WriteLine("=== 1. Generating Windows App & Installer Icons ===");
        string appIco = Path.Combine(appAssetsDir, "app.ico");
        string installerIco = Path.Combine(installerDir, "app.ico");
        SaveMultiSizeIco(appIco, new[] { 256, 128, 64, 48, 32, 16 });
        File.Copy(appIco, installerIco, true);
        Console.WriteLine($"Generated app.ico at {appIco}");

        CreateWizardLarge(Path.Combine(installerDir, "wizard_large.bmp"));
        CreateWizardSmall(Path.Combine(installerDir, "wizard_small.bmp"));
        Console.WriteLine("Generated Inno Setup wizard banners.");

        Console.WriteLine("\n=== 2. Generating Android Mipmap Launcher Icons ===");
        SaveAndroidLauncherIcon(Path.Combine(androidResDir, "mipmap-mdpi", "ic_launcher.png"), 48);
        SaveAndroidLauncherIcon(Path.Combine(androidResDir, "mipmap-hdpi", "ic_launcher.png"), 72);
        SaveAndroidLauncherIcon(Path.Combine(androidResDir, "mipmap-xhdpi", "ic_launcher.png"), 96);
        SaveAndroidLauncherIcon(Path.Combine(androidResDir, "mipmap-xxhdpi", "ic_launcher.png"), 144);
        SaveAndroidLauncherIcon(Path.Combine(androidResDir, "mipmap-xxxhdpi", "ic_launcher.png"), 192);
        Console.WriteLine("Generated Android mipmap launcher icons.");

        Console.WriteLine("\n=== 3. Generating Microsoft Store Visual Assets ===");
        GenerateMicrosoftStoreAssets(msStoreDir);
        Console.WriteLine("Generated Microsoft Store logos, box arts, posters, and 1920x1080 screenshots.");

        Console.WriteLine("\n=== 4. Generating Google Play Store Visual Assets ===");
        GeneratePlayStoreAssets(playStoreDir);
        Console.WriteLine("Generated Google Play Store 512x512 icon, 1024x500 banner, and 1080x2400 screenshots.");
    }

    // Modern "Print Ofo" Logo with stylized printer and glowing wireless connection
    static Bitmap DrawPrintOfoIcon(int size, bool solidBackground = false)
    {
        var bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

        if (solidBackground)
        {
            using var bgSolid = new SolidBrush(Color.FromArgb(255, 15, 23, 42)); // Slate 900
            g.FillRectangle(bgSolid, 0, 0, size, size);
        }
        else
        {
            g.Clear(Color.Transparent);
        }

        float s = size / 256f;

        // Rounded background card (Royal Indigo to Cyan Gradient)
        var bgRect = new RectangleF(10 * s, 10 * s, 236 * s, 236 * s);
        using (var brush = new LinearGradientBrush(bgRect, Color.FromArgb(255, 30, 58, 138), Color.FromArgb(255, 14, 116, 144), 45f))
        using (var path = GetRoundedRect(bgRect, 44 * s))
        {
            g.FillPath(brush, path);
            using var borderPen = new Pen(Color.FromArgb(120, 255, 255, 255), Math.Max(1f, 2.5f * s));
            g.DrawPath(borderPen, path);
        }

        // Wi-Fi wireless arcs
        using (var wavePen = new Pen(Color.FromArgb(255, 56, 189, 248), Math.Max(2f, 6.5f * s)))
        {
            wavePen.StartCap = LineCap.Round;
            wavePen.EndCap = LineCap.Round;
            g.DrawArc(wavePen, 74 * s, 26 * s, 108 * s, 62 * s, 200, 140);
            g.DrawArc(wavePen, 94 * s, 42 * s, 68 * s, 42 * s, 200, 140);
        }
        using (var dotBrush = new SolidBrush(Color.White))
        {
            g.FillEllipse(dotBrush, 122 * s, 66 * s, 12 * s, 12 * s);
        }

        // Upper Paper Sheet
        var paperRect = new RectangleF(78 * s, 90 * s, 100 * s, 42 * s);
        using (var paperBrush = new SolidBrush(Color.FromArgb(255, 248, 250, 252)))
        {
            g.FillRectangle(paperBrush, paperRect);
        }
        using (var linePen = new Pen(Color.FromArgb(200, 148, 163, 184), Math.Max(1f, 2 * s)))
        {
            g.DrawLine(linePen, 90 * s, 102 * s, 166 * s, 102 * s);
            g.DrawLine(linePen, 90 * s, 114 * s, 146 * s, 114 * s);
        }

        // Printer Body
        var bodyRect = new RectangleF(48 * s, 122 * s, 160 * s, 74 * s);
        using (var bodyPath = GetRoundedRect(bodyRect, 16 * s))
        using (var bodyBrush = new SolidBrush(Color.White))
        using (var bodyPen = new Pen(Color.FromArgb(220, 226, 232, 240), Math.Max(1f, 1.5f * s)))
        {
            g.FillPath(bodyBrush, bodyPath);
            g.DrawPath(bodyPen, bodyPath);
        }

        // Output Slot
        using (var slotBrush = new SolidBrush(Color.FromArgb(255, 15, 23, 42)))
        {
            g.FillRectangle(slotBrush, 66 * s, 150 * s, 124 * s, 18 * s);
        }

        // Printed Output Paper
        var outPaperRect = new RectangleF(76 * s, 160 * s, 104 * s, 48 * s);
        using (var outBrush = new SolidBrush(Color.FromArgb(255, 241, 245, 249)))
        {
            g.FillRectangle(outBrush, outPaperRect);
        }
        using (var outLinePen = new Pen(Color.FromArgb(200, 148, 163, 184), Math.Max(1f, 2 * s)))
        {
            g.DrawLine(outLinePen, 86 * s, 174 * s, 170 * s, 174 * s);
            g.DrawLine(outLinePen, 86 * s, 186 * s, 152 * s, 186 * s);
            g.DrawLine(outLinePen, 86 * s, 196 * s, 130 * s, 196 * s);
        }

        // Brand Badge: "OFO" in small bold letters on bottom right of printer
        if (size >= 64)
        {
            using var ofoFont = new Font("Segoe UI", 8.5f * s, FontStyle.Bold);
            using var ofoBrush = new SolidBrush(Color.FromArgb(255, 2, 132, 199));
            g.DrawString("OFO", ofoFont, ofoBrush, 58 * s, 128 * s);
        }

        // Green Power LED
        using (var ledBrush = new SolidBrush(Color.FromArgb(255, 34, 197, 94)))
        {
            g.FillEllipse(ledBrush, 190 * s, 134 * s, 9 * s, 9 * s);
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

    static void SaveAndroidLauncherIcon(string path, int size)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        using var bmp = DrawPrintOfoIcon(size, false);
        bmp.Save(path, ImageFormat.Png);
    }

    static void SaveMultiSizeIco(string outPath, int[] sizes)
    {
        var entries = new List<(int size, byte[] bytes)>();
        foreach (int s in sizes)
        {
            using var bmp = DrawPrintOfoIcon(s);
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
            bw.Write((byte)0);
            bw.Write((byte)0);
            bw.Write((ushort)1);
            bw.Write((ushort)32);
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

        using var icon = DrawPrintOfoIcon(80);
        g.DrawImage(icon, 42, 38, 80, 80);

        using var titleFont = new Font("Segoe UI", 13, FontStyle.Bold);
        using var subFont = new Font("Segoe UI", 8.5f, FontStyle.Regular);
        using var textBrush = new SolidBrush(Color.White);
        using var cyanBrush = new SolidBrush(Color.FromArgb(255, 56, 189, 248));
        using var sf = new StringFormat { Alignment = StringAlignment.Center };

        g.DrawString("Print Ofo", titleFont, textBrush, 82, 132, sf);
        g.DrawString("Windows Print Server", subFont, cyanBrush, 82, 158, sf);
        g.DrawString("High-Speed Wireless\nPrinting for Mobile & PC", subFont, textBrush, 82, 210, sf);

        bmp.Save(outPath, ImageFormat.Bmp);
    }

    static void CreateWizardSmall(string outPath)
    {
        using var bmp = new Bitmap(55, 58, PixelFormat.Format24bppRgb);
        using var g = Graphics.FromImage(bmp);
        g.Clear(Color.White);

        using var icon = DrawPrintOfoIcon(48);
        g.DrawImage(icon, 3, 5, 48, 48);

        bmp.Save(outPath, ImageFormat.Bmp);
    }

    // =========================================================================
    // MICROSOFT STORE ASSETS GENERATOR
    // =========================================================================
    static void GenerateMicrosoftStoreAssets(string dir)
    {
        // 1. Store Logos
        using (var bmp = DrawPrintOfoIcon(300)) { bmp.Save(Path.Combine(dir, "StoreLogo.png"), ImageFormat.Png); }
        using (var bmp = DrawPrintOfoIcon(150)) { bmp.Save(Path.Combine(dir, "Square150x150Logo.png"), ImageFormat.Png); }
        using (var bmp = DrawPrintOfoIcon(44))  { bmp.Save(Path.Combine(dir, "Square44x44Logo.png"), ImageFormat.Png); }
        using (var bmp = DrawPrintOfoIcon(310)) { bmp.Save(Path.Combine(dir, "LargeTile310x310Logo.png"), ImageFormat.Png); }

        // 2. Wide 310x150 Tile
        using (var bmp = new Bitmap(310, 150, PixelFormat.Format32bppArgb))
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using var brush = new LinearGradientBrush(new Rectangle(0, 0, 310, 150), Color.FromArgb(255, 15, 23, 42), Color.FromArgb(255, 30, 58, 138), 45f);
            g.FillRectangle(brush, 0, 0, 310, 150);
            using var icon = DrawPrintOfoIcon(96);
            g.DrawImage(icon, 18, 27, 96, 96);
            using var font = new Font("Segoe UI", 14f, FontStyle.Bold);
            using var subFont = new Font("Segoe UI", 9f, FontStyle.Regular);
            using var whiteBrush = new SolidBrush(Color.White);
            using var cyanBrush = new SolidBrush(Color.FromArgb(255, 56, 189, 248));
            g.DrawString("Print Ofo", font, whiteBrush, 126, 44);
            g.DrawString("Windows Print Server", subFont, cyanBrush, 126, 74);
            bmp.Save(Path.Combine(dir, "Wide310x150Logo.png"), ImageFormat.Png);
        }

        // 3. Splash Screen (620x300)
        using (var bmp = new Bitmap(620, 300, PixelFormat.Format32bppArgb))
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using var brush = new LinearGradientBrush(new Rectangle(0, 0, 620, 300), Color.FromArgb(255, 15, 23, 42), Color.FromArgb(255, 30, 58, 138), 45f);
            g.FillRectangle(brush, 0, 0, 620, 300);
            using var icon = DrawPrintOfoIcon(128);
            g.DrawImage(icon, 100, 86, 128, 128);
            using var font = new Font("Segoe UI", 26f, FontStyle.Bold);
            using var subFont = new Font("Segoe UI", 12f, FontStyle.Regular);
            using var whiteBrush = new SolidBrush(Color.White);
            using var cyanBrush = new SolidBrush(Color.FromArgb(255, 56, 189, 248));
            g.DrawString("Print Ofo", font, whiteBrush, 250, 105);
            g.DrawString("Windows Wireless Print Server", subFont, cyanBrush, 252, 155);
            bmp.Save(Path.Combine(dir, "SplashScreen620x300.png"), ImageFormat.Png);
        }

        // 4. BoxArt (1:1 Aspect Ratio) 1080x1080 and 2160x2160
        CreateBoxArt(Path.Combine(dir, "BoxArt_1080x1080.png"), 1080);
        CreateBoxArt(Path.Combine(dir, "BoxArt_2160x2160.png"), 2160);

        // 5. Poster (2:3 Aspect Ratio) 720x1080 and 1440x2160
        CreatePosterArt(Path.Combine(dir, "Poster_720x1080.png"), 720, 1080);
        CreatePosterArt(Path.Combine(dir, "Poster_1440x2160.png"), 1440, 2160);

        // 6. 4x Desktop Screenshots (1920x1080)
        CreateDesktopScreenshot(Path.Combine(dir, "Screenshot_1_Dashboard_1920x1080.png"), 1);
        CreateDesktopScreenshot(Path.Combine(dir, "Screenshot_2_Printers_1920x1080.png"), 2);
        CreateDesktopScreenshot(Path.Combine(dir, "Screenshot_3_Pairing_Security_1920x1080.png"), 3);
        CreateDesktopScreenshot(Path.Combine(dir, "Screenshot_4_SpoolerQueue_1920x1080.png"), 4);
    }

    static void CreateBoxArt(string outPath, int size)
    {
        using var bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

        float s = size / 1080f;

        // Dark modern background gradient
        using var bgBrush = new LinearGradientBrush(new Rectangle(0, 0, size, size), Color.FromArgb(255, 15, 23, 42), Color.FromArgb(255, 10, 15, 29), 90f);
        g.FillRectangle(bgBrush, 0, 0, size, size);

        // Glow circle in center
        using (var pth = new GraphicsPath())
        {
            pth.AddEllipse(size * 0.15f, size * 0.15f, size * 0.7f, size * 0.7f);
            using var pgb = new PathGradientBrush(pth)
            {
                CenterColor = Color.FromArgb(80, 2, 132, 199),
                SurroundColors = new[] { Color.Transparent }
            };
            g.FillPath(pgb, pth);
        }

        // Central Icon
        int iconSize = (int)(440 * s);
        using var icon = DrawPrintOfoIcon(iconSize);
        g.DrawImage(icon, (size - iconSize) / 2, (int)(180 * s), iconSize, iconSize);

        // Title and description
        using var titleFont = new Font("Segoe UI", 58 * s, FontStyle.Bold);
        using var subFont = new Font("Segoe UI", 26 * s, FontStyle.Regular);
        using var tagFont = new Font("Segoe UI", 20 * s, FontStyle.Regular);
        using var whiteBrush = new SolidBrush(Color.White);
        using var cyanBrush = new SolidBrush(Color.FromArgb(255, 56, 189, 248));
        using var goldBrush = new SolidBrush(Color.FromArgb(255, 250, 204, 21));
        using var sf = new StringFormat { Alignment = StringAlignment.Center };

        g.DrawString("Print Ofo", titleFont, whiteBrush, size / 2, 690 * s, sf);
        g.DrawString("Wireless Print Server for Windows", subFont, cyanBrush, size / 2, 770 * s, sf);
        g.DrawString("Print from Android & PC • USB Canon & Epson Support • 100% Offline", tagFont, goldBrush, size / 2, 830 * s, sf);

        bmp.Save(outPath, ImageFormat.Png);
    }

    static void CreatePosterArt(string outPath, int w, int h)
    {
        using var bmp = new Bitmap(w, h, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

        float s = w / 720f;

        // Dark modern background gradient
        using var bgBrush = new LinearGradientBrush(new Rectangle(0, 0, w, h), Color.FromArgb(255, 15, 23, 42), Color.FromArgb(255, 30, 58, 138), 90f);
        g.FillRectangle(bgBrush, 0, 0, w, h);

        int iconSize = (int)(320 * s);
        using var icon = DrawPrintOfoIcon(iconSize);
        g.DrawImage(icon, (w - iconSize) / 2, (int)(180 * s), iconSize, iconSize);

        using var titleFont = new Font("Segoe UI", 50 * s, FontStyle.Bold);
        using var subFont = new Font("Segoe UI", 22 * s, FontStyle.Bold);
        using var bodyFont = new Font("Segoe UI", 16 * s, FontStyle.Regular);
        using var whiteBrush = new SolidBrush(Color.White);
        using var cyanBrush = new SolidBrush(Color.FromArgb(255, 56, 189, 248));
        using var sf = new StringFormat { Alignment = StringAlignment.Center };

        g.DrawString("Print Ofo", titleFont, whiteBrush, w / 2, 560 * s, sf);
        g.DrawString("WINDOWS PRINT SERVER", subFont, cyanBrush, w / 2, 640 * s, sf);

        g.DrawString("High-Speed Wireless Local Printing\nConnect Android Phone over Wi-Fi\nDirect to Canon & Epson USB Printers\nZero Cloud Dependency • Fast & Secure", bodyFont, whiteBrush, w / 2, 720 * s, sf);

        bmp.Save(outPath, ImageFormat.Png);
    }

    static void CreateDesktopScreenshot(string outPath, int screenIndex)
    {
        int w = 1920;
        int h = 1080;
        using var bmp = new Bitmap(w, h, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

        // Background
        using var bgBrush = new SolidBrush(Color.FromArgb(255, 15, 23, 42)); // Slate 900
        g.FillRectangle(bgBrush, 0, 0, w, h);

        // Header Card
        var headerRect = new Rectangle(50, 40, 1820, 100);
        using var cardBrush = new SolidBrush(Color.FromArgb(255, 30, 41, 59)); // Slate 800
        using var cardPen = new Pen(Color.FromArgb(255, 51, 65, 85), 2);
        g.FillRectangle(cardBrush, headerRect);
        g.DrawRectangle(cardPen, headerRect);

        // OFO Logo badge in header
        using var badgeBrush = new SolidBrush(Color.FromArgb(255, 2, 132, 199));
        g.FillRectangle(badgeBrush, 75, 58, 65, 65);
        using var ofoFont = new Font("Segoe UI", 20, FontStyle.Bold);
        using var whiteBrush = new SolidBrush(Color.White);
        g.DrawString("OFO", ofoFont, whiteBrush, 78, 72);

        using var titleFont = new Font("Segoe UI", 24, FontStyle.Bold);
        using var subFont = new Font("Segoe UI", 13, FontStyle.Regular);
        using var textMuted = new SolidBrush(Color.FromArgb(255, 148, 163, 184));
        using var cyanBrush = new SolidBrush(Color.FromArgb(255, 56, 189, 248));
        using var greenBrush = new SolidBrush(Color.FromArgb(255, 34, 197, 94));
        using var amberBrush = new SolidBrush(Color.FromArgb(255, 251, 191, 36));

        g.DrawString("PRINT OFO SERVER", titleFont, whiteBrush, 160, 56);
        g.DrawString("Local Wi-Fi Print Engine for Windows & Android — High-Performance Native Spooler", subFont, textMuted, 162, 100);

        // Status Badge
        g.FillEllipse(greenBrush, 1580, 80, 16, 16);
        using var statusFont = new Font("Segoe UI", 16, FontStyle.Bold);
        g.DrawString("SERVER ACTIVE (PORT 8080 / 8443)", statusFont, greenBrush, 1610, 74);

        // Left Column (Pairing & Server Info) - Width 540
        var leftRect = new Rectangle(50, 165, 540, 840);
        g.FillRectangle(cardBrush, leftRect);
        g.DrawRectangle(cardPen, leftRect);

        using var secTitleFont = new Font("Segoe UI", 15, FontStyle.Bold);
        g.DrawString("DEVICE PAIRING & SECURITY", secTitleFont, cyanBrush, 80, 190);

        // QR Code box
        var qrBox = new Rectangle(80, 240, 220, 220);
        g.FillRectangle(new SolidBrush(Color.White), qrBox);
        // Draw decorative QR pattern
        using (var qrBrush = new SolidBrush(Color.Black))
        {
            g.FillRectangle(qrBrush, 95, 255, 60, 60);
            g.FillRectangle(qrBrush, 225, 255, 60, 60);
            g.FillRectangle(qrBrush, 95, 385, 60, 60);
            g.FillRectangle(new SolidBrush(Color.White), 110, 270, 30, 30);
            g.FillRectangle(new SolidBrush(Color.White), 240, 270, 30, 30);
            g.FillRectangle(new SolidBrush(Color.White), 110, 400, 30, 30);
            g.FillRectangle(qrBrush, 120, 280, 10, 10);
            g.FillRectangle(qrBrush, 250, 280, 10, 10);
            g.FillRectangle(qrBrush, 120, 410, 10, 10);
            // Some random QR blocks
            for (int i = 0; i < 8; i++)
            {
                g.FillRectangle(qrBrush, 170 + (i % 3) * 15, 270 + (i * 18), 12, 12);
                g.FillRectangle(qrBrush, 195 + (i % 2) * 20, 370 + ((7 - i) * 10), 12, 12);
            }
        }

        // PIN display
        g.DrawString("PAIRING PIN", subFont, textMuted, 320, 250);
        using var pinFont = new Font("Consolas", 38, FontStyle.Bold);
        g.DrawString("629401", pinFont, cyanBrush, 315, 280);
        g.DrawString("Valid for: 14m 32s", subFont, amberBrush, 320, 350);
        g.DrawString("Scan with Print Ofo Android App\nor enter PIN manually", subFont, textMuted, 320, 390);

        // Network Info
        g.DrawString("NETWORK STATUS", secTitleFont, cyanBrush, 80, 500);
        using var bodyFont = new Font("Segoe UI", 13, FontStyle.Regular);
        using var valFont = new Font("Segoe UI", 13, FontStyle.Bold);

        g.DrawString("Wi-Fi IP Address:", bodyFont, textMuted, 80, 545);
        g.DrawString("10.134.68.221 (Primary)", valFont, whiteBrush, 240, 545);

        g.DrawString("HTTP Port:", bodyFont, textMuted, 80, 585);
        g.DrawString("8080 (Active)", valFont, whiteBrush, 240, 585);

        g.DrawString("HTTPS Port:", bodyFont, textMuted, 80, 625);
        g.DrawString("8443 (TLS Encrypted)", valFont, whiteBrush, 240, 625);

        g.DrawString("Firewall Rule:", bodyFont, textMuted, 80, 665);
        g.DrawString("Allowed (Private & Public)", valFont, greenBrush, 240, 665);

        g.DrawString("Connected Devices:", bodyFont, textMuted, 80, 705);
        g.DrawString("1 Mobile Phone (Paired)", valFont, cyanBrush, 240, 705);

        // Buttons on bottom left
        g.FillRectangle(new SolidBrush(Color.FromArgb(255, 2, 132, 199)), 80, 780, 220, 50);
        g.DrawString("Print Test Page (300 DPI)", new Font("Segoe UI", 11, FontStyle.Bold), whiteBrush, 98, 795);

        g.FillRectangle(new SolidBrush(Color.FromArgb(255, 71, 85, 105)), 320, 780, 220, 50);
        g.DrawString("View Server Logs", new Font("Segoe UI", 11, FontStyle.Bold), whiteBrush, 360, 795);

        // Right Column (Printers & Spooler Queue) - Width 1250
        var rightRect = new Rectangle(620, 165, 1250, 840);
        g.FillRectangle(cardBrush, rightRect);
        g.DrawRectangle(cardPen, rightRect);

        g.DrawString("CONNECTED HARDWARE PRINTERS", secTitleFont, cyanBrush, 650, 190);

        // Printer 1: Canon
        var prn1Rect = new Rectangle(650, 235, 580, 130);
        g.FillRectangle(new SolidBrush(Color.FromArgb(255, 15, 23, 42)), prn1Rect);
        g.DrawRectangle(cardPen, prn1Rect);
        g.FillEllipse(greenBrush, 675, 260, 14, 14);
        g.DrawString("Canon MF4800 Series UFRII LT", new Font("Segoe UI", 16, FontStyle.Bold), whiteBrush, 705, 252);
        g.DrawString("USB Monochrome Laser • Duplex Enabled • Ready", subFont, textMuted, 705, 285);
        g.DrawString("Spooler Queue: 0 pending • Status: Online & Idle", subFont, greenBrush, 705, 315);

        // Printer 2: Epson
        var prn2Rect = new Rectangle(1260, 235, 580, 130);
        g.FillRectangle(new SolidBrush(Color.FromArgb(255, 15, 23, 42)), prn2Rect);
        g.DrawRectangle(cardPen, prn2Rect);
        g.FillEllipse(greenBrush, 1285, 260, 14, 14);
        g.DrawString("EPSON L3210 Series", new Font("Segoe UI", 16, FontStyle.Bold), whiteBrush, 1315, 252);
        g.DrawString("USB Color InkTank • Photo & Document • Ready", subFont, textMuted, 1315, 285);
        g.DrawString("Spooler Queue: 0 pending • Status: Online & Idle", subFont, greenBrush, 1315, 315);

        // Live Spooler Queue
        g.DrawString("LIVE PRINT JOB QUEUE & REAL-TIME SPOOLER MONITOR", secTitleFont, cyanBrush, 650, 400);

        // Queue Table Header
        g.FillRectangle(new SolidBrush(Color.FromArgb(255, 51, 65, 85)), 650, 440, 1190, 45);
        using var tableHeaderFont = new Font("Segoe UI", 12, FontStyle.Bold);
        g.DrawString("JOB ID", tableHeaderFont, whiteBrush, 670, 452);
        g.DrawString("DOCUMENT", tableHeaderFont, whiteBrush, 770, 452);
        g.DrawString("PRINTER", tableHeaderFont, whiteBrush, 1070, 452);
        g.DrawString("PAGES / COPIES", tableHeaderFont, whiteBrush, 1320, 452);
        g.DrawString("STATUS", tableHeaderFont, whiteBrush, 1500, 452);
        g.DrawString("TIME", tableHeaderFont, whiteBrush, 1720, 452);

        // Row 1 (Completed job from phone)
        var row1Rect = new Rectangle(650, 495, 1190, 55);
        g.FillRectangle(new SolidBrush(Color.FromArgb(255, 15, 23, 42)), row1Rect);
        g.DrawString("#1048", bodyFont, cyanBrush, 670, 510);
        g.DrawString("contract_agreement_final.pdf", valFont, whiteBrush, 770, 510);
        g.DrawString("Canon MF4800 Series", bodyFont, textMuted, 1070, 510);
        g.DrawString("4 Pages • 1 Copy", bodyFont, whiteBrush, 1320, 510);
        g.DrawString("✔ Printed Successfully", valFont, greenBrush, 1500, 510);
        g.DrawString("15:52:10", bodyFont, textMuted, 1720, 510);

        // Row 2 (Completed image job from gallery share)
        var row2Rect = new Rectangle(650, 560, 1190, 55);
        g.FillRectangle(new SolidBrush(Color.FromArgb(255, 20, 30, 50)), row2Rect);
        g.DrawString("#1049", bodyFont, cyanBrush, 670, 575);
        g.DrawString("IMG-20261026-WA0022.jpg", valFont, whiteBrush, 770, 575);
        g.DrawString("EPSON L3210 Series", bodyFont, textMuted, 1070, 575);
        g.DrawString("Photo (Color) • 1 Copy", bodyFont, whiteBrush, 1320, 575);
        g.DrawString("✔ Printed Successfully", valFont, greenBrush, 1500, 575);
        g.DrawString("15:53:44", bodyFont, textMuted, 1720, 575);

        // Row 3 (Diagnostic Test Page)
        var row3Rect = new Rectangle(650, 625, 1190, 55);
        g.FillRectangle(new SolidBrush(Color.FromArgb(255, 15, 23, 42)), row3Rect);
        g.DrawString("#1050", bodyFont, cyanBrush, 670, 640);
        g.DrawString("Print_Ofo_Diagnostic_Page.png", valFont, whiteBrush, 770, 640);
        g.DrawString("Canon MF4800 Series", bodyFont, textMuted, 1070, 640);
        g.DrawString("1 Page • 300 DPI", bodyFont, whiteBrush, 1320, 640);
        g.DrawString("✔ Printed Successfully", valFont, greenBrush, 1500, 640);
        g.DrawString("15:54:12", bodyFont, textMuted, 1720, 640);

        // Server Audit Log Box on bottom right
        g.DrawString("RECENT SECURITY & TRANSACTION LOGS", secTitleFont, cyanBrush, 650, 710);
        var logRect = new Rectangle(650, 745, 1190, 230);
        g.FillRectangle(new SolidBrush(Color.FromArgb(255, 10, 15, 26)), logRect);
        g.DrawRectangle(cardPen, logRect);
        using var logFont = new Font("Consolas", 11, FontStyle.Regular);

        g.DrawString("[15:51:00] [INFO] [App] Print Ofo Server v1.0.0 listening on 0.0.0.0:8080 (HTTP) & :8443 (HTTPS)", logFont, cyanBrush, 665, 760);
        g.DrawString("[15:51:02] [INFO] [PrinterDiscovery] Found 2 physical USB printers: Canon MF4800, EPSON L3210", logFont, whiteBrush, 665, 785);
        g.DrawString("[15:52:05] [INFO] [Auth] Mobile client connected via QR Code. Issued bearer token for device.", logFont, greenBrush, 665, 810);
        g.DrawString("[15:52:08] [INFO] [JobEndpoints] Received multipart document 'contract_agreement_final.pdf' (1.42 MB)", logFont, whiteBrush, 665, 835);
        g.DrawString("[15:52:10] [INFO] [SpoolTracker] Spooler Job #1048 transitioned: Spooling -> Printing -> Completed", logFont, greenBrush, 665, 860);
        g.DrawString("[15:53:40] [INFO] [JobEndpoints] Received image 'IMG-20261026-WA0022.jpg' from Android Gallery share sheet", logFont, whiteBrush, 665, 885);
        g.DrawString("[15:53:44] [INFO] [SpoolTracker] Spooler Job #1049 sent to EPSON L3210. 100% printed.", logFont, greenBrush, 665, 910);
        g.DrawString("[15:54:12] [INFO] [PrintProcessor] 300 DPI test page completed on Canon MF4800.", logFont, cyanBrush, 665, 935);

        // Footer banner
        g.DrawString("Print Ofo v1.0.0 — Microsoft Store & Production Edition — Fast, Local & Private", subFont, textMuted, 50, 1030);

        bmp.Save(outPath, ImageFormat.Png);
    }

    // =========================================================================
    // GOOGLE PLAY STORE ASSETS GENERATOR
    // =========================================================================
    static void GeneratePlayStoreAssets(string dir)
    {
        // 1. Play Store App Icon (512x512 PNG, 32-bit, Flat solid background, no alpha)
        using (var bmp = DrawPrintOfoIcon(512, true))
        {
            bmp.Save(Path.Combine(dir, "playstore_icon_512x512.png"), ImageFormat.Png);
        }

        // 2. Play Store Feature Graphic (1024x500 PNG)
        using (var bmp = new Bitmap(1024, 500, PixelFormat.Format32bppArgb))
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

            using var brush = new LinearGradientBrush(new Rectangle(0, 0, 1024, 500), Color.FromArgb(255, 15, 23, 42), Color.FromArgb(255, 30, 58, 138), 45f);
            g.FillRectangle(brush, 0, 0, 1024, 500);

            // Icon on left
            using var icon = DrawPrintOfoIcon(240);
            g.DrawImage(icon, 70, 130, 240, 240);

            using var titleFont = new Font("Segoe UI", 48, FontStyle.Bold);
            using var subFont = new Font("Segoe UI", 20, FontStyle.Bold);
            using var bodyFont = new Font("Segoe UI", 16, FontStyle.Regular);
            using var whiteBrush = new SolidBrush(Color.White);
            using var cyanBrush = new SolidBrush(Color.FromArgb(255, 56, 189, 248));
            using var goldBrush = new SolidBrush(Color.FromArgb(255, 250, 204, 21));

            g.DrawString("Print Ofo", titleFont, whiteBrush, 360, 130);
            g.DrawString("WIRELESS MOBILE PRINTING FOR ANDROID", subFont, cyanBrush, 365, 215);
            g.DrawString("Print PDF & Images to USB Printers over Local Wi-Fi\nOne-Tap QR Code Connect • Live Spooler Tracking\n100% Offline • No Cloud Required", bodyFont, whiteBrush, 365, 270);
            g.DrawString("⚡ Works with Canon, Epson, HP & all Windows Printers", bodyFont, goldBrush, 365, 370);

            bmp.Save(Path.Combine(dir, "playstore_feature_graphic_1024x500.png"), ImageFormat.Png);
        }

        // 3. 4x Phone Screenshots (1080x2400)
        CreatePhoneScreenshot(Path.Combine(dir, "Phone_Screenshot_1_Home_1080x2400.png"), 1);
        CreatePhoneScreenshot(Path.Combine(dir, "Phone_Screenshot_2_Scanner_Connect_1080x2400.png"), 2);
        CreatePhoneScreenshot(Path.Combine(dir, "Phone_Screenshot_3_Preview_Settings_1080x2400.png"), 3);
        CreatePhoneScreenshot(Path.Combine(dir, "Phone_Screenshot_4_Spooler_Tracking_1080x2400.png"), 4);
    }

    static void CreatePhoneScreenshot(string outPath, int screenType)
    {
        int w = 1080;
        int h = 2400;
        using var bmp = new Bitmap(w, h, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

        // Background
        using var bgBrush = new SolidBrush(Color.FromArgb(255, 15, 23, 42)); // Slate 900
        g.FillRectangle(bgBrush, 0, 0, w, h);

        // Top Status Bar (Battery, Wi-Fi, Time)
        using var statusFont = new Font("Segoe UI", 26, FontStyle.Bold);
        using var whiteBrush = new SolidBrush(Color.White);
        using var cyanBrush = new SolidBrush(Color.FromArgb(255, 56, 189, 248));
        using var greenBrush = new SolidBrush(Color.FromArgb(255, 34, 197, 94));
        using var textMuted = new SolidBrush(Color.FromArgb(255, 148, 163, 184));
        using var cardBrush = new SolidBrush(Color.FromArgb(255, 30, 41, 59));
        using var cardPen = new Pen(Color.FromArgb(255, 51, 65, 85), 3);

        g.DrawString("10:45", statusFont, whiteBrush, 60, 50);
        g.DrawString("5G  Wi-Fi  98%", statusFont, whiteBrush, 820, 50);

        // App Bar
        var appBarRect = new Rectangle(0, 110, w, 150);
        g.FillRectangle(cardBrush, appBarRect);

        using var appIcon = DrawPrintOfoIcon(80);
        g.DrawImage(appIcon, 60, 145, 80, 80);

        using var appTitleFont = new Font("Segoe UI", 42, FontStyle.Bold);
        using var subTitleFont = new Font("Segoe UI", 22, FontStyle.Regular);

        g.DrawString("Print Ofo", appTitleFont, whiteBrush, 165, 150);
        g.DrawString("Wireless Mobile Print Client", subTitleFont, cyanBrush, 170, 210);

        if (screenType == 1) // Home Screen
        {
            // Connection Card
            var connCard = new Rectangle(60, 310, 960, 280);
            g.FillRectangle(cardBrush, connCard);
            g.DrawRectangle(cardPen, connCard);

            g.FillEllipse(greenBrush, 100, 355, 24, 24);
            g.DrawString("CONNECTED TO PC PRINT SERVER", new Font("Segoe UI", 24, FontStyle.Bold), greenBrush, 140, 350);
            g.DrawString("Laptop Wi-Fi: 10.134.68.221:8080\nSecurity: Authorized via PIN • Local Wi-Fi Network", new Font("Segoe UI", 22, FontStyle.Regular), textMuted, 100, 400);

            // Printer Status Card
            var prnCard = new Rectangle(60, 630, 960, 340);
            g.FillRectangle(cardBrush, prnCard);
            g.DrawRectangle(cardPen, prnCard);

            g.DrawString("AVAILABLE USB PRINTERS (2)", new Font("Segoe UI", 24, FontStyle.Bold), cyanBrush, 100, 670);

            // Printer 1
            g.FillEllipse(greenBrush, 100, 735, 18, 18);
            g.DrawString("Canon MF4800 Series UFRII LT", new Font("Segoe UI", 26, FontStyle.Bold), whiteBrush, 135, 725);
            g.DrawString("Monochrome Laser • Duplex • Ready", new Font("Segoe UI", 20, FontStyle.Regular), textMuted, 135, 765);

            // Printer 2
            g.FillEllipse(greenBrush, 100, 835, 18, 18);
            g.DrawString("EPSON L3210 Series", new Font("Segoe UI", 26, FontStyle.Bold), whiteBrush, 135, 825);
            g.DrawString("Color InkTank • Photo & Document • Ready", new Font("Segoe UI", 20, FontStyle.Regular), textMuted, 135, 865);

            // Big Print Action Buttons
            var btn1 = new Rectangle(60, 1020, 960, 160);
            using var btnBrush = new LinearGradientBrush(btn1, Color.FromArgb(255, 2, 132, 199), Color.FromArgb(255, 30, 58, 138), 45f);
            g.FillRectangle(btnBrush, btn1);
            g.DrawString("📄 Select & Print PDF Document", new Font("Segoe UI", 32, FontStyle.Bold), whiteBrush, 180, 1070);

            var btn2 = new Rectangle(60, 1220, 960, 160);
            using var btn2Brush = new LinearGradientBrush(btn2, Color.FromArgb(255, 14, 116, 144), Color.FromArgb(255, 15, 118, 110), 45f);
            g.FillRectangle(btn2Brush, btn2);
            g.DrawString("🖼️ Select & Print Photos / Images", new Font("Segoe UI", 32, FontStyle.Bold), whiteBrush, 180, 1270);

            // Recent Jobs Section
            g.DrawString("RECENT PRINT JOBS", new Font("Segoe UI", 26, FontStyle.Bold), cyanBrush, 60, 1440);

            var job1 = new Rectangle(60, 1500, 960, 170);
            g.FillRectangle(cardBrush, job1);
            g.DrawRectangle(cardPen, job1);
            g.DrawString("contract_agreement_final.pdf", new Font("Segoe UI", 26, FontStyle.Bold), whiteBrush, 100, 1530);
            g.DrawString("Canon MF4800 • 4 Pages • 1 Copy", new Font("Segoe UI", 20, FontStyle.Regular), textMuted, 100, 1580);
            g.DrawString("✔ Printed", new Font("Segoe UI", 24, FontStyle.Bold), greenBrush, 780, 1550);

            var job2 = new Rectangle(60, 1700, 960, 170);
            g.FillRectangle(cardBrush, job2);
            g.DrawRectangle(cardPen, job2);
            g.DrawString("IMG-20261026-WA0022.jpg", new Font("Segoe UI", 26, FontStyle.Bold), whiteBrush, 100, 1730);
            g.DrawString("EPSON L3210 • Color Photo • 1 Copy", new Font("Segoe UI", 20, FontStyle.Regular), textMuted, 100, 1780);
            g.DrawString("✔ Printed", new Font("Segoe UI", 24, FontStyle.Bold), greenBrush, 780, 1750);
        }
        else if (screenType == 2) // Connect Screen (QR Code Scanner)
        {
            g.DrawString("CONNECT TO WINDOWS SERVER", new Font("Segoe UI", 28, FontStyle.Bold), cyanBrush, 60, 310);
            g.DrawString("Scan the dynamic QR code displayed on Print Ofo Windows Server", new Font("Segoe UI", 20, FontStyle.Regular), textMuted, 60, 360);

            // Camera Viewfinder Box
            var viewFinder = new Rectangle(140, 440, 800, 800);
            g.FillRectangle(new SolidBrush(Color.Black), viewFinder);
            using var cyanThickPen = new Pen(Color.FromArgb(255, 56, 189, 248), 8);
            // Corner marks
            g.DrawLine(cyanThickPen, 140, 440, 240, 440); g.DrawLine(cyanThickPen, 140, 440, 140, 540);
            g.DrawLine(cyanThickPen, 940, 440, 840, 440); g.DrawLine(cyanThickPen, 940, 440, 940, 540);
            g.DrawLine(cyanThickPen, 140, 1240, 240, 1240); g.DrawLine(cyanThickPen, 140, 1240, 140, 1140);
            g.DrawLine(cyanThickPen, 940, 1240, 840, 1240); g.DrawLine(cyanThickPen, 940, 1240, 940, 1140);

            // Red laser scan line
            using var laserPen = new Pen(Color.FromArgb(200, 239, 68, 68), 6);
            g.DrawLine(laserPen, 160, 840, 920, 840);

            g.DrawString("OR ENTER DETAILS MANUALLY", new Font("Segoe UI", 26, FontStyle.Bold), whiteBrush, 60, 1330);

            // Manual inputs
            var input1 = new Rectangle(60, 1400, 960, 120);
            g.FillRectangle(cardBrush, input1);
            g.DrawRectangle(cardPen, input1);
            g.DrawString("IP Address: 10.134.68.221", new Font("Segoe UI", 26, FontStyle.Regular), whiteBrush, 90, 1440);

            var input2 = new Rectangle(60, 1550, 960, 120);
            g.FillRectangle(cardBrush, input2);
            g.DrawRectangle(cardPen, input2);
            g.DrawString("Pairing PIN: 6 2 9 4 0 1", new Font("Segoe UI", 26, FontStyle.Bold), cyanBrush, 90, 1590);

            var connBtn = new Rectangle(60, 1730, 960, 140);
            g.FillRectangle(new SolidBrush(Color.FromArgb(255, 2, 132, 199)), connBtn);
            g.DrawString("Connect to Laptop Server", new Font("Segoe UI", 30, FontStyle.Bold), whiteBrush, 280, 1775);
        }
        else if (screenType == 3) // Preview & Settings
        {
            g.DrawString("PRINT PREVIEW & SETTINGS", new Font("Segoe UI", 28, FontStyle.Bold), cyanBrush, 60, 310);

            // Document Preview Card
            var prevCard = new Rectangle(60, 380, 960, 580);
            g.FillRectangle(new SolidBrush(Color.White), prevCard);
            g.DrawRectangle(cardPen, prevCard);
            g.DrawString("ANNUAL PERFORMANCE REPORT 2026", new Font("Segoe UI", 24, FontStyle.Bold), new SolidBrush(Color.Black), 100, 420);
            g.DrawString("Page 1 of 4 • Format: PDF Vector Document", new Font("Segoe UI", 18, FontStyle.Regular), new SolidBrush(Color.Gray), 100, 460);
            // Draw dummy report lines
            for (int i = 0; i < 8; i++)
            {
                g.FillRectangle(new SolidBrush(Color.FromArgb(255, 226, 232, 240)), 100, 520 + (i * 45), 880, 25);
            }

            // Printer Selection Card
            var prnSelect = new Rectangle(60, 1000, 960, 140);
            g.FillRectangle(cardBrush, prnSelect);
            g.DrawRectangle(cardPen, prnSelect);
            g.DrawString("TARGET PRINTER", new Font("Segoe UI", 18, FontStyle.Bold), textMuted, 90, 1020);
            g.DrawString("Canon MF4800 Series UFRII LT (USB Laser)", new Font("Segoe UI", 24, FontStyle.Bold), whiteBrush, 90, 1065);

            // Settings Grid
            var opt1 = new Rectangle(60, 1170, 460, 130);
            g.FillRectangle(cardBrush, opt1);
            g.DrawRectangle(cardPen, opt1);
            g.DrawString("COPIES", new Font("Segoe UI", 18, FontStyle.Bold), textMuted, 90, 1190);
            g.DrawString("1 Copy", new Font("Segoe UI", 26, FontStyle.Bold), cyanBrush, 90, 1235);

            var opt2 = new Rectangle(560, 1170, 460, 130);
            g.FillRectangle(cardBrush, opt2);
            g.DrawRectangle(cardPen, opt2);
            g.DrawString("COLOR MODE", new Font("Segoe UI", 18, FontStyle.Bold), textMuted, 590, 1190);
            g.DrawString("Monochrome (B&W)", new Font("Segoe UI", 24, FontStyle.Bold), whiteBrush, 590, 1235);

            var opt3 = new Rectangle(60, 1330, 460, 130);
            g.FillRectangle(cardBrush, opt3);
            g.DrawRectangle(cardPen, opt3);
            g.DrawString("ORIENTATION", new Font("Segoe UI", 18, FontStyle.Bold), textMuted, 90, 1350);
            g.DrawString("Portrait", new Font("Segoe UI", 24, FontStyle.Bold), whiteBrush, 90, 1395);

            var opt4 = new Rectangle(560, 1330, 460, 130);
            g.FillRectangle(cardBrush, opt4);
            g.DrawRectangle(cardPen, opt4);
            g.DrawString("SCALING", new Font("Segoe UI", 18, FontStyle.Bold), textMuted, 590, 1350);
            g.DrawString("Fit to Printable Page", new Font("Segoe UI", 24, FontStyle.Bold), whiteBrush, 590, 1395);

            // Big Print Button
            var submitBtn = new Rectangle(60, 1530, 960, 160);
            using var submitGrad = new LinearGradientBrush(submitBtn, Color.FromArgb(255, 34, 197, 94), Color.FromArgb(255, 21, 128, 61), 45f);
            g.FillRectangle(submitGrad, submitBtn);
            g.DrawString("🖨️ SEND TO PRINTER NOW", new Font("Segoe UI", 32, FontStyle.Bold), whiteBrush, 240, 1585);
        }
        else if (screenType == 4) // Spooler Tracking
        {
            g.DrawString("PRINT JOB STATUS & SPOOLER", new Font("Segoe UI", 28, FontStyle.Bold), cyanBrush, 60, 310);

            var dialog = new Rectangle(60, 420, 960, 1100);
            g.FillRectangle(cardBrush, dialog);
            g.DrawRectangle(cardPen, dialog);

            using var bigCheck = DrawPrintOfoIcon(180);
            g.DrawImage(bigCheck, 450, 480, 180, 180);

            g.DrawString("PRINTING IN PROGRESS", new Font("Segoe UI", 36, FontStyle.Bold), whiteBrush, 280, 700);
            g.DrawString("Job #1048 is being spooled to Canon MF4800", new Font("Segoe UI", 22, FontStyle.Regular), textMuted, 270, 770);

            // Progress Bar
            var barBg = new Rectangle(120, 850, 840, 30);
            g.FillRectangle(new SolidBrush(Color.FromArgb(255, 15, 23, 42)), barBg);
            var barFill = new Rectangle(120, 850, 630, 30);
            g.FillRectangle(new SolidBrush(Color.FromArgb(255, 34, 197, 94)), barFill);

            g.DrawString("75% Completed (Page 3 of 4)", new Font("Segoe UI", 24, FontStyle.Bold), cyanBrush, 360, 910);

            // Job Details Table
            g.DrawString("File Name:", new Font("Segoe UI", 22, FontStyle.Regular), textMuted, 120, 1000);
            g.DrawString("contract_agreement_final.pdf", new Font("Segoe UI", 22, FontStyle.Bold), whiteBrush, 340, 1000);

            g.DrawString("File Size:", new Font("Segoe UI", 22, FontStyle.Regular), textMuted, 120, 1060);
            g.DrawString("1.42 MB (Vector High-Res)", new Font("Segoe UI", 22, FontStyle.Bold), whiteBrush, 340, 1060);

            g.DrawString("Printer:", new Font("Segoe UI", 22, FontStyle.Regular), textMuted, 120, 1120);
            g.DrawString("Canon MF4800 Series UFRII LT", new Font("Segoe UI", 22, FontStyle.Bold), whiteBrush, 340, 1120);

            g.DrawString("Spooler Status:", new Font("Segoe UI", 22, FontStyle.Regular), textMuted, 120, 1180);
            g.DrawString("Printing — Transmitting to hardware...", new Font("Segoe UI", 22, FontStyle.Bold), greenBrush, 340, 1180);

            // Cancel Button
            var cancelBtn = new Rectangle(120, 1320, 840, 120);
            g.FillRectangle(new SolidBrush(Color.FromArgb(255, 239, 68, 68)), cancelBtn);
            g.DrawString("Cancel Print Job", new Font("Segoe UI", 26, FontStyle.Bold), whiteBrush, 410, 1355);
        }

        bmp.Save(outPath, ImageFormat.Png);
    }
}
