using ReisingerIntelliApp_V4.Models;

namespace ReisingerIntelliApp_V4.Services;

public class PdfStorageService
{
    private readonly PdfConversionService _conversionService;

    public PdfStorageService(PdfConversionService conversionService)
    {
        _conversionService = conversionService;
    }

    public static string Sanitize(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return "_";
        var invalid = Path.GetInvalidFileNameChars();
        var cleaned = new string(input.Select(c => invalid.Contains(c) ? '_' : c).ToArray());
        // Collapse spaces and dots
        cleaned = cleaned.Trim().Replace(" ", "_");
        return cleaned;
    }

    public async Task<string> EnsureFloorFolderAsync(Building b, Floor f)
    {
        var root = FileSystem.AppDataDirectory;
        var path = Path.Combine(root, "floorplans", Sanitize(b.BuildingName), Sanitize(f.FloorName));
        Directory.CreateDirectory(path);
        await Task.CompletedTask;
        return path;
    }

    public async Task<(string pdfPath, string? pngPath)> ImportPdfAsync(Building b, Floor f, FileResult file, bool generatePreviewPng = true)
    {
        if (file == null) throw new ArgumentNullException(nameof(file));
        var folder = await EnsureFloorFolderAsync(b, f);
        var targetPdf = Path.Combine(folder, "plan.pdf");

        // Copy PDF
        await using (var src = await file.OpenReadAsync())
        await using (var dst = File.Create(targetPdf))
        {
            await src.CopyToAsync(dst);
        }

        string? pngPath = null;
        if (generatePreviewPng)
        {
            try
            {
                var targetPng = Path.Combine(folder, "preview_page1.png");
                pngPath = await _conversionService.ConvertFirstPageToPngAsync(targetPdf, targetPng, 144);
            }
            catch
            {
                // Best-effort preview generation; ignore failures.
                pngPath = null;
            }
        }

        return (targetPdf, pngPath);
    }

    public async Task DeleteFloorAssetsAsync(Building b, Floor f)
    {
        try
        {
            var folder = Path.Combine(FileSystem.AppDataDirectory, "floorplans", Sanitize(b.BuildingName), Sanitize(f.FloorName));
            var pdf = Path.Combine(folder, "plan.pdf");
            var png = Path.Combine(folder, "preview_page1.png");

            if (File.Exists(pdf)) File.Delete(pdf);
            if (File.Exists(png)) File.Delete(png);

            // Cleanup folder if empty
            if (Directory.Exists(folder) && !Directory.EnumerateFileSystemEntries(folder).Any())
            {
                Directory.Delete(folder, true);
            }

            // Attempt parent cleanup
            var buildingFolder = Path.GetDirectoryName(folder);
            if (!string.IsNullOrEmpty(buildingFolder) && Directory.Exists(buildingFolder) && !Directory.EnumerateFileSystemEntries(buildingFolder).Any())
            {
                Directory.Delete(buildingFolder, true);
            }
        }
        catch
        {
            // Ignore cleanup errors for now
        }
        await Task.CompletedTask;
    }
}
