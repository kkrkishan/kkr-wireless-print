using System.Drawing;
using System.IO;
using Windows.Data.Pdf;
using Windows.Storage;
using Windows.Storage.Streams;
using KKR.PrintServer.Core.Logging;

namespace KKR.PrintServer.Core.Services;

public class PdfRenderer : IDisposable
{
    private readonly string _filePath;
    private PdfDocument? _pdfDocument;

    public uint PageCount => _pdfDocument?.PageCount ?? 0;

    public PdfRenderer(string filePath)
    {
        _filePath = filePath;
    }

    public async Task InitializeAsync()
    {
        if (!File.Exists(_filePath))
        {
            throw new FileNotFoundException("PDF file not found.", _filePath);
        }

        string absolutePath = Path.GetFullPath(_filePath);
        var storageFile = await StorageFile.GetFileFromPathAsync(absolutePath);
        _pdfDocument = await PdfDocument.LoadFromFileAsync(storageFile);
        AppLogger.Instance.Info("PdfRenderer", $"Loaded PDF '{Path.GetFileName(_filePath)}' ({_pdfDocument.PageCount} pages).");
    }

    public SizeF GetPageSize(uint pageIndex)
    {
        if (_pdfDocument == null) throw new InvalidOperationException("PDF document is not loaded.");
        if (pageIndex >= _pdfDocument.PageCount) throw new ArgumentOutOfRangeException(nameof(pageIndex));

        using var page = _pdfDocument.GetPage(pageIndex);
        return new SizeF((float)page.Size.Width, (float)page.Size.Height);
    }

    public async Task<Bitmap> RenderPageToBitmapAsync(uint pageIndex, int targetDpi = 300)
    {
        if (_pdfDocument == null) throw new InvalidOperationException("PDF document is not loaded.");
        if (pageIndex >= _pdfDocument.PageCount) throw new ArgumentOutOfRangeException(nameof(pageIndex));

        using var page = _pdfDocument.GetPage(pageIndex);

        // Windows.Data.Pdf dimensions are in 96-DPI device-independent pixels (DIPs).
        // 8.5 x 11 inches at 96 DPI = 816 x 1056 DIPs.
        // Target pixel dimensions at targetDpi (e.g., 300 DPI for high-quality printing):
        double scale = targetDpi / 96.0;
        uint targetWidth = (uint)Math.Max(1, Math.Round(page.Size.Width * scale));
        uint targetHeight = (uint)Math.Max(1, Math.Round(page.Size.Height * scale));

        using var inMemoryStream = new InMemoryRandomAccessStream();
        var options = new PdfPageRenderOptions
        {
            DestinationWidth = targetWidth,
            DestinationHeight = targetHeight
        };

        await page.RenderToStreamAsync(inMemoryStream, options);
        inMemoryStream.Seek(0);

        using var netStream = inMemoryStream.AsStreamForRead();
        using var tempBitmap = new Bitmap(netStream);

        // Explicit pixel dimensions with GraphicsUnit.Pixel to prevent display DPI scaling
        var bitmap = new Bitmap((int)targetWidth, (int)targetHeight, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        bitmap.SetResolution(targetDpi, targetDpi);

        using (var g = Graphics.FromImage(bitmap))
        {
            g.Clear(Color.White);
            g.DrawImage(tempBitmap, new Rectangle(0, 0, (int)targetWidth, (int)targetHeight),
                        0, 0, tempBitmap.Width, tempBitmap.Height, GraphicsUnit.Pixel);
        }

        return bitmap;
    }

    public void Dispose()
    {
        _pdfDocument = null;
    }
}
