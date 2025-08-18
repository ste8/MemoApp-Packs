using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ImagesGenerator.Configuration;

namespace ImagesGenerator.Services;

public interface IFileService
{
    Task<string> SaveImageAsync(byte[] imageData, string fileName, CancellationToken cancellationToken = default);
    void EnsureOutputDirectoryExists();
    string SanitizeFileName(string fileName);
}

public class FileService : IFileService
{
    private readonly ILogger<FileService> _logger;
    private readonly ImageGenerationSettings _settings;
    private readonly string _outputDirectory;
    
    public FileService(
        IOptions<ImageGenerationSettings> settings,
        ILogger<FileService> logger)
    {
        _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _outputDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _settings.OutputDirectory);
    }
    
    public void EnsureOutputDirectoryExists()
    {
        if (!Directory.Exists(_outputDirectory))
        {
            Directory.CreateDirectory(_outputDirectory);
            _logger.LogInformation("Created output directory: {OutputDirectory}", _outputDirectory);
        }
    }
    
    public async Task<string> SaveImageAsync(byte[] imageData, string fileName, CancellationToken cancellationToken = default)
    {
        var sanitizedFileName = SanitizeFileName(fileName);
        var filePath = Path.Combine(_outputDirectory, sanitizedFileName);
        
        await File.WriteAllBytesAsync(filePath, imageData, cancellationToken);
        _logger.LogInformation("Saved image: {FileName}", sanitizedFileName);
        
        return filePath;
    }
    
    public string SanitizeFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = string.Join("", fileName.Split(invalidChars));
        
        if (string.IsNullOrWhiteSpace(sanitized))
        {
            sanitized = "image.png";
        }
        else if (!sanitized.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
        {
            sanitized += ".png";
        }
        
        return sanitized;
    }
}