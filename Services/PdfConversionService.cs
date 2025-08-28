namespace ReisingerIntelliApp_V4.Services;

public class PdfConversionService
{
    // Android implementation uses PdfRenderer to render the first page to a PNG.
    // Other platforms fall back to a no-op stub and simply return the output path.
    public virtual async Task<string> ConvertFirstPageToPngAsync(string pdfPath, string outputPngPath, int dpi = 144)
    {
        if (string.IsNullOrWhiteSpace(pdfPath)) throw new ArgumentException("pdfPath is required", nameof(pdfPath));
        if (!File.Exists(pdfPath)) throw new FileNotFoundException("PDF not found", pdfPath);

#if ANDROID
    // Ensure directory exists
    string? dir = Path.GetDirectoryName(outputPngPath);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

        // Scale factor: Android's PdfRenderer renders at 72 DPI by default; scale up to requested DPI
        float scale = dpi / 72f;

    using var pfd = global::Android.OS.ParcelFileDescriptor.Open(new global::Java.IO.File(pdfPath), global::Android.OS.ParcelFileMode.ReadOnly);
        using var renderer = new global::Android.Graphics.Pdf.PdfRenderer(pfd);
        if (renderer.PageCount <= 0)
        {
            throw new InvalidOperationException("PDF contains no pages");
        }
        using var page = renderer.OpenPage(0);

        int width = (int)(page.Width * scale);
        int height = (int)(page.Height * scale);
        if (width <= 0 || height <= 0)
        {
            width = Math.Max(page.Width, 1);
            height = Math.Max(page.Height, 1);
        }

        using var bitmap = global::Android.Graphics.Bitmap.CreateBitmap(width, height, global::Android.Graphics.Bitmap.Config.Argb8888);
        using (var canvas = new global::Android.Graphics.Canvas(bitmap))
        {
            canvas.DrawColor(global::Android.Graphics.Color.White);
        }

        using (var matrix = new global::Android.Graphics.Matrix())
        {
            matrix.SetScale(scale, scale);
            page.Render(bitmap, null, matrix, global::Android.Graphics.Pdf.PdfRenderMode.ForDisplay);
        }

        using (var fs = File.Open(outputPngPath, FileMode.Create, FileAccess.Write, FileShare.Read))
        {
            bitmap.Compress(global::Android.Graphics.Bitmap.CompressFormat.Png, 100, fs);
            await fs.FlushAsync();
        }

        return outputPngPath;
#else
        await Task.CompletedTask;
        return outputPngPath;
#endif
    }
}
