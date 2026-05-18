using Microsoft.AspNetCore.Http;

namespace GLMS.Enterprise.Core.Interfaces;

public interface IFileService
{
    bool ValidatePdf(IFormFile? file);
    Task<(bool success, string filePath, string errorMessage)> SavePdfAsync(IFormFile file, string uploadFolder);
    string GetFileUrl(string filePath);
    bool FileExists(string filePath);
    void DeleteFile(string filePath);
}
