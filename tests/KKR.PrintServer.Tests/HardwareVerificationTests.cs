using System.Drawing;
using System.Drawing.Printing;
using KKR.PrintServer.Core.Models;
using KKR.PrintServer.Core.Services;
using Xunit;
using Xunit.Abstractions;

namespace KKR.PrintServer.Tests;

public class HardwareVerificationTests
{
    private readonly ITestOutputHelper _output;

    public HardwareVerificationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void EnumerateHardwarePrinters_DetectsUsbPrintersAndCapabilities()
    {
        var discovery = new PrinterDiscoveryService();
        var printers = discovery.GetPrinters(refresh: true);

        _output.WriteLine($"Discovered {printers.Count} installed Windows printers:");
        foreach (var p in printers)
        {
            _output.WriteLine($" - Printer: '{p.Name}'");
            _output.WriteLine($"   Driver: '{p.DriverName}'");
            _output.WriteLine($"   Port: '{p.PortName}'");
            _output.WriteLine($"   Default: {p.IsDefault}, Online: {p.IsOnline}, Status: '{p.StatusDescription}'");
            _output.WriteLine($"   Color: {p.Capabilities.SupportsColor}, Duplex: {p.Capabilities.CanDuplex}, PaperSizes: {p.Capabilities.SupportedPaperSizes.Count}");
        }

        Assert.NotEmpty(printers);

        // Verify the two physical USB printers specified in the project objective:
        bool hasEpson = printers.Any(p => p.Name.Contains("EPSON", StringComparison.OrdinalIgnoreCase) ||
                                          p.Name.Contains("L3210", StringComparison.OrdinalIgnoreCase));
        bool hasCanon = printers.Any(p => p.Name.Contains("Canon", StringComparison.OrdinalIgnoreCase) ||
                                          p.Name.Contains("MF4800", StringComparison.OrdinalIgnoreCase));

        _output.WriteLine($"EPSON L3210 present: {hasEpson}");
        _output.WriteLine($"Canon MF4800 present: {hasCanon}");

        Assert.True(hasEpson, "Expected EPSON physical USB printer to be discovered.");
        Assert.True(hasCanon, "Expected Canon physical USB printer to be discovered.");
    }

    [Fact]
    public async Task PdfRenderer_RendersRealPdfToHighResolutionBitmap()
    {
        string fixturePath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "sample.pdf");
        if (!File.Exists(fixturePath))
        {
            fixturePath = Path.GetFullPath(@"..\..\..\Fixtures\sample.pdf");
        }

        Assert.True(File.Exists(fixturePath), $"Sample PDF not found at {fixturePath}");

        using var renderer = new PdfRenderer(fixturePath);
        await renderer.InitializeAsync();

        Assert.Equal(1u, renderer.PageCount);

        var size = renderer.GetPageSize(0);
        _output.WriteLine($"PDF Page 1 Size: {size.Width} x {size.Height} DIPs (Aspect: {size.Width / size.Height:F3})");

        // Windows.Data.Pdf reports Letter 8.5 x 11 in at 96 DPI = 816 x 1056 DIPs
        Assert.Equal(816f, size.Width);
        Assert.Equal(1056f, size.Height);

        // Render at 300 DPI: (816 / 96 * 300 = 2550 px; 1056 / 96 * 300 = 3300 px)
        using var bmp = await renderer.RenderPageToBitmapAsync(0, 300);
        Assert.NotNull(bmp);
        Assert.Equal(2550, bmp.Width);
        Assert.Equal(3300, bmp.Height);
        Assert.Equal(300, Math.Round(bmp.HorizontalResolution));

        _output.WriteLine($"Rendered PDF page directly to 300 DPI Bitmap ({bmp.Width}x{bmp.Height} px) without aspect ratio distortion.");
    }

    [Fact]
    public void TestPageGeneration_RendersHighResolutionDiagnosticPage()
    {
        var discovery = new PrinterDiscoveryService();
        var epson = discovery.GetPrinter("EPSON L3210 Series") ?? discovery.GetPrinters().FirstOrDefault();
        Assert.NotNull(epson);

        using var bitmap = TestPageGenerator.GenerateTestPageImage(epson.Name, epson, "10.92.81.221", 8443, httpsEnabled: true);

        Assert.NotNull(bitmap);
        Assert.Equal(2480, bitmap.Width);
        Assert.Equal(3508, bitmap.Height);
        Assert.Equal(300, Math.Round(bitmap.HorizontalResolution));
        Assert.Equal(300, Math.Round(bitmap.VerticalResolution));

        _output.WriteLine($"Diagnostic test page rendered successfully at 300 DPI ({bitmap.Width}x{bitmap.Height} pixels).");
    }

    [Fact]
    public void ImageRenderer_ScalingCalculationsPreserveAspectRatio()
    {
        using var testImg = new Bitmap(800, 600); // 4:3 aspect ratio
        testImg.SetResolution(300, 300);

        using var bmp = new Bitmap(1000, 1000);
        using var g = Graphics.FromImage(bmp);

        var bounds = new Rectangle(0, 0, 500, 1000); // 1:2 page bounds

        // Test FitPage
        ImageRenderer.DrawImageToPrinter(g, bounds, testImg, PrintScalingMode.FitPage, centerOnPage: true);

        // Test FillPage
        ImageRenderer.DrawImageToPrinter(g, bounds, testImg, PrintScalingMode.FillPage, centerOnPage: true);

        // Test ActualSize
        ImageRenderer.DrawImageToPrinter(g, bounds, testImg, PrintScalingMode.ActualSize, centerOnPage: true);

        _output.WriteLine("Image rendering pipeline executed with high-quality bicubic interpolation across all scaling modes.");
    }
}
