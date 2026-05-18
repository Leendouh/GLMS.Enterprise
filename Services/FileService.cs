using GLMS.Enterprise.Core.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace GLMS.Enterprise.Services;

/// <summary>
/// Implements PDF file validation and storage.
/// - Validates extension, MIME type, size (max 10 MB), and PDF magic bytes (%PDF)
/// - Saves with UUID prefix to prevent overwrites
/// - Returns relative URL for use in download links
/// </summary>
public class FileService : IFileService
{
    private readonly ILogger<FileService> _logger;
    private const long MaxFileSizeBytes = 10L * 1024 * 1024; // 10 MB
    private static readonly string[] AllowedExtensions = { ".pdf" };
    private static readonly string[] AllowedMimeTypes  = { "application/pdf" };
    private static readonly byte[]   PdfMagicBytes     = { 0x25, 0x50, 0x44, 0x46 }; // %PDF

    public FileService(ILogger<FileService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Returns true only if the file passes all four validation checks:
    /// extension, MIME type, size, and PDF magic bytes.
    /// </summary>
    public bool ValidatePdf(IFormFile? file)
    {
        if (file == null || file.Length == 0)
            return false;

        // 1. Extension check (case-insensitive)
        var ext = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(ext))
            return false;
        if (!AllowedExtensions.Contains(ext.ToLowerInvariant()))
            return false;

        // 2. MIME type check
        if (!AllowedMimeTypes.Contains(file.ContentType.ToLowerInvariant()))
            return false;

        // 3. Size check
        if (file.Length > MaxFileSizeBytes)
            return false;

        // 4. PDF magic bytes check (%PDF)
        try
        {
            using var stream = file.OpenReadStream();
            var header = new byte[4];
            var bytesRead = stream.Read(header, 0, 4);
            if (bytesRead < 4) return false;
            if (!header.SequenceEqual(PdfMagicBytes)) return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not read PDF header bytes.");
            return false;
        }

        return true;
    }

    /// <summary>
    /// Saves the PDF to uploadFolder using {Guid}_{OriginalFileName}.pdf naming.
    /// Creates the directory if it does not exist.
    /// </summary>
    public async Task<(bool success, string filePath, string errorMessage)> SavePdfAsync(
        IFormFile file, string uploadFolder)
    {
        try
        {
            if (!ValidatePdf(file))
                return (false, string.Empty, "Invalid file. Only PDF files up to 10 MB are allowed.");

            Directory.CreateDirectory(uploadFolder);

            var safeOriginal = Path.GetFileName(file.FileName); // strip any path traversal
            var uniqueName   = $"{Guid.NewGuid()}_{safeOriginal}";
            var fullPath     = Path.Combine(uploadFolder, uniqueName);

            await using var stream = new FileStream(fullPath, FileMode.Create);
            await file.CopyToAsync(stream);

            _logger.LogInformation("PDF saved: {Path}", fullPath);
            return (true, uniqueName, string.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save PDF file.");
            return (false, string.Empty, "An error occurred while saving the file.");
        }
    }

    /// <summary>Returns a relative URL path for the stored file.</summary>
    public string GetFileUrl(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath)) return string.Empty;
        // filePath is just the filename portion; prepend the URL segment
        return $"/Uploads/Contracts/{filePath}";
    }

    public bool FileExists(string filePath) => File.Exists(filePath);

    public void DeleteFile(string filePath)
    {
        if (File.Exists(filePath))
            File.Delete(filePath);
    }
}
