using GLMS.Enterprise.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text;
using Xunit;

namespace GLMS.Enterprise.Tests.File;

public class FileServiceTests
{
    private readonly Mock<ILogger<FileService>> _mockLogger;
    private readonly FileService _fileService;

    public FileServiceTests()
    {
        _mockLogger = new Mock<ILogger<FileService>>();
        _fileService = new FileService(_mockLogger.Object);
    }

    [Fact]
    public void ValidatePdf_NullFile_ReturnsFalse()
    {
        // Arrange
        IFormFile? file = null;

        // Act
        var result = _fileService.ValidatePdf(file);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ValidatePdf_EmptyFile_ReturnsFalse()
    {
        // Arrange
        var file = CreateMockFormFile("", 0, "application/pdf");

        // Act
        var result = _fileService.ValidatePdf(file);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ValidatePdf_ExeFile_ReturnsFalse()
    {
        // Arrange
        var content = Encoding.UTF8.GetBytes("MZ");
        var file = CreateMockFormFile("malware.exe", content.Length, "application/x-msdownload", content);

        // Act
        var result = _fileService.ValidatePdf(file);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ValidatePdf_JpgFile_ReturnsFalse()
    {
        // Arrange
        var content = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 }; // JPEG magic bytes
        var file = CreateMockFormFile("image.jpg", content.Length, "image/jpeg", content);

        // Act
        var result = _fileService.ValidatePdf(file);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ValidatePdf_FileWithoutExtension_ReturnsFalse()
    {
        // Arrange
        var content = Encoding.UTF8.GetBytes("test");
        var file = CreateMockFormFile("file", content.Length, "application/pdf", content);

        // Act
        var result = _fileService.ValidatePdf(file);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ValidatePdf_UppercasePDF_ReturnsTrue()
    {
        // Arrange
        var content = new byte[] { 0x25, 0x50, 0x44, 0x46 }; // PDF magic bytes
        var file = CreateMockFormFile("CONTRACT.PDF", content.Length, "application/pdf", content);

        // Act
        var result = _fileService.ValidatePdf(file);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ValidatePdf_Lowercasepdf_ReturnsTrue()
    {
        // Arrange
        var content = new byte[] { 0x25, 0x50, 0x44, 0x46 }; // PDF magic bytes
        var file = CreateMockFormFile("document.pdf", content.Length, "application/pdf", content);

        // Act
        var result = _fileService.ValidatePdf(file);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ValidatePdf_ValidPdf_ReturnsTrue()
    {
        // Arrange
        var content = new byte[] { 0x25, 0x50, 0x44, 0x46 }; // PDF magic bytes
        var file = CreateMockFormFile("contract.pdf", content.Length, "application/pdf", content);

        // Act
        var result = _fileService.ValidatePdf(file);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ValidatePdf_FileTooLarge_ReturnsFalse()
    {
        // Arrange
        var content = new byte[15 * 1024 * 1024]; // 15 MB
        var file = CreateMockFormFile("large.pdf", content.Length, "application/pdf", content);

        // Act
        var result = _fileService.ValidatePdf(file);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ValidatePdf_InvalidMimeType_ReturnsFalse()
    {
        // Arrange
        var content = new byte[] { 0x25, 0x50, 0x44, 0x46 }; // PDF magic bytes
        var file = CreateMockFormFile("document.pdf", content.Length, "text/plain", content);

        // Act
        var result = _fileService.ValidatePdf(file);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ValidatePdf_InvalidMagicBytes_ReturnsFalse()
    {
        // Arrange
        var content = Encoding.UTF8.GetBytes("Not a PDF");
        var file = CreateMockFormFile("fake.pdf", content.Length, "application/pdf", content);

        // Act
        var result = _fileService.ValidatePdf(file);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ValidatePdf_MaxSizeBoundary_ReturnsTrue()
    {
        // Arrange
        var content = new byte[10 * 1024 * 1024]; // Exactly 10 MB
        content[0] = 0x25; content[1] = 0x50; content[2] = 0x44; content[3] = 0x46; // PDF magic bytes
        var file = CreateMockFormFile("boundary.pdf", content.Length, "application/pdf", content);

        // Act
        var result = _fileService.ValidatePdf(file);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ValidatePdf_JustOverMaxSize_ReturnsFalse()
    {
        // Arrange
        var content = new byte[10 * 1024 * 1024 + 1]; // 10 MB + 1 byte
        content[0] = 0x25; content[1] = 0x50; content[2] = 0x44; content[3] = 0x46; // PDF magic bytes
        var file = CreateMockFormFile("over.pdf", content.Length, "application/pdf", content);

        // Act
        var result = _fileService.ValidatePdf(file);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ValidatePdf_TxtExtensionWithPdfContent_ReturnsFalse()
    {
        // Arrange
        var content = new byte[] { 0x25, 0x50, 0x44, 0x46 }; // PDF magic bytes
        var file = CreateMockFormFile("document.txt", content.Length, "application/pdf", content);

        // Act
        var result = _fileService.ValidatePdf(file);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task SavePdfAsync_ValidPdf_SavesWithGuidPrefix()
    {
        var content = new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x34 };
        var file = CreateMockFormFile("agreement.pdf", content.Length, "application/pdf", content);
        var folder = Path.Combine(Path.GetTempPath(), "glms-tests", Guid.NewGuid().ToString());

        try
        {
            var (success, filePath, error) = await _fileService.SavePdfAsync(file, folder);

            Assert.True(success);
            Assert.Empty(error);
            Assert.Matches(@"^[0-9a-fA-F-]{36}_agreement\.pdf$", filePath);
            Assert.True(System.IO.File.Exists(Path.Combine(folder, filePath)));
        }
        finally
        {
            if (Directory.Exists(folder))
                Directory.Delete(folder, true);
        }
    }

    private IFormFile CreateMockFormFile(string fileName, long length, string contentType, byte[]? content = null)
    {
        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.FileName).Returns(fileName);
        mockFile.Setup(f => f.Length).Returns(length);
        mockFile.Setup(f => f.ContentType).Returns(contentType);
        
        if (content != null)
        {
            var stream = new MemoryStream(content);
            mockFile.Setup(f => f.OpenReadStream()).Returns(stream);
        }
        else
        {
            mockFile.Setup(f => f.OpenReadStream()).Returns(new MemoryStream());
        }

        return mockFile.Object;
    }
}
