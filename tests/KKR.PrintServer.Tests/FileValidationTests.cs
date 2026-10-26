using System.Text;
using KKR.PrintServer.Core.Models;
using KKR.PrintServer.Core.Services;
using Xunit;

namespace KKR.PrintServer.Tests;

public class FileValidationTests
{
    private readonly SecurityService _service;

    public FileValidationTests()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), "kkr_file_test_" + Guid.NewGuid().ToString("N"));
        _service = new SecurityService(tempDir);
    }

    [Fact]
    public void ValidateUploadedDocument_WithValidPdf_ReturnsValid()
    {
        byte[] pdfBytes = Encoding.ASCII.GetBytes("%PDF-1.4\nSome sample PDF stream contents...");
        using var ms = new MemoryStream(pdfBytes);

        var result = _service.ValidateUploadedDocument(ms, "report.pdf", ms.Length, 50 * 1024 * 1024);

        Assert.True(result.IsValid);
        Assert.Equal(PrintDocumentType.Pdf, result.DetectedType);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public void ValidateUploadedDocument_WithValidPng_ReturnsValid()
    {
        byte[] pngHeader = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D };
        using var ms = new MemoryStream(pngHeader);

        var result = _service.ValidateUploadedDocument(ms, "photo.png", ms.Length, 50 * 1024 * 1024);

        Assert.True(result.IsValid);
        Assert.Equal(PrintDocumentType.Png, result.DetectedType);
    }

    [Fact]
    public void ValidateUploadedDocument_WithValidJpeg_ReturnsValid()
    {
        byte[] jpegHeader = { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46 };
        using var ms = new MemoryStream(jpegHeader);

        var result = _service.ValidateUploadedDocument(ms, "camera.jpg", ms.Length, 50 * 1024 * 1024);

        Assert.True(result.IsValid);
        Assert.Equal(PrintDocumentType.Jpeg, result.DetectedType);
    }

    [Fact]
    public void ValidateUploadedDocument_WithZeroBytes_ReturnsInvalid()
    {
        using var ms = new MemoryStream();
        var result = _service.ValidateUploadedDocument(ms, "empty.pdf", 0, 50 * 1024 * 1024);

        Assert.False(result.IsValid);
        Assert.Contains("0 bytes", result.ErrorMessage);
    }

    [Fact]
    public void ValidateUploadedDocument_ExceedingLimit_ReturnsInvalid()
    {
        byte[] pdfBytes = Encoding.ASCII.GetBytes("%PDF-1.4\nLarge contents...");
        using var ms = new MemoryStream(pdfBytes);

        var result = _service.ValidateUploadedDocument(ms, "huge.pdf", 100_000_000, 10_000_000);

        Assert.False(result.IsValid);
        Assert.Contains("exceeds", result.ErrorMessage);
    }

    [Fact]
    public void ValidateUploadedDocument_WithForgedExtension_ReturnsInvalid()
    {
        // An executable or random text named .pdf
        byte[] fakeBytes = Encoding.ASCII.GetBytes("MZThisIsNotAPdfDocument");
        using var ms = new MemoryStream(fakeBytes);

        var result = _service.ValidateUploadedDocument(ms, "trojan.pdf", ms.Length, 50 * 1024 * 1024);

        Assert.False(result.IsValid);
        Assert.Contains("invalid PDF magic header", result.ErrorMessage);
    }
}
