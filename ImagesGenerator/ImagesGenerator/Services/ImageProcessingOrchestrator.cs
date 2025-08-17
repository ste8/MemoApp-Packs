using Microsoft.Extensions.Logging;
using ImagesGenerator.Models;

namespace ImagesGenerator.Services;

public interface IImageProcessingOrchestrator
{
    Task<int> ProcessAllImagesAsync(CancellationToken cancellationToken = default);
}

public class ImageProcessingOrchestrator : IImageProcessingOrchestrator
{
    private readonly IImageGenerationService _imageGenerationService;
    private readonly IFileService _fileService;
    private readonly ISubjectService _subjectService;
    private readonly ILogger<ImageProcessingOrchestrator> _logger;
    
    public ImageProcessingOrchestrator(
        IImageGenerationService imageGenerationService,
        IFileService fileService,
        ISubjectService subjectService,
        ILogger<ImageProcessingOrchestrator> logger)
    {
        _imageGenerationService = imageGenerationService ?? throw new ArgumentNullException(nameof(imageGenerationService));
        _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
        _subjectService = subjectService ?? throw new ArgumentNullException(nameof(subjectService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    public async Task<int> ProcessAllImagesAsync(CancellationToken cancellationToken = default)
    {
        _fileService.EnsureOutputDirectoryExists();
        
        var subjects = _subjectService.GetSubjects();
        var results = new List<ImageGenerationResult>();
        
        foreach (var subject in subjects)
        {
            _logger.LogInformation("Processing: {Subject}", subject);
            
            try
            {
                var imageData = await _imageGenerationService.GenerateImageAsync(subject, cancellationToken);
                
                if (imageData != null)
                {
                    var filePath = await _fileService.SaveImageAsync(imageData, subject.FileName, cancellationToken);
                    results.Add(ImageGenerationResult.CreateSuccess(subject, filePath));
                    _logger.LogInformation("Successfully processed: {Subject}", subject);
                }
                else
                {
                    results.Add(ImageGenerationResult.CreateFailure(subject, "Failed to generate image after all retries"));
                    _logger.LogError("Failed to generate image for {Subject} after all retries", subject);
                }
            }
            catch (Exception ex)
            {
                results.Add(ImageGenerationResult.CreateFailure(subject, ex.Message));
                _logger.LogError(ex, "Error processing {Subject}", subject);
            }
        }
        
        return ReportResults(results);
    }
    
    private int ReportResults(List<ImageGenerationResult> results)
    {
        var failedResults = results.Where(r => !r.Success).ToList();
        
        if (failedResults.Any())
        {
            _logger.LogError("Failed to generate {Count} image(s):", failedResults.Count);
            foreach (var failure in failedResults)
            {
                _logger.LogError("  - {Subject}: {Error}", failure.Subject, failure.ErrorMessage);
            }
            return 1;
        }
        
        _logger.LogInformation("All images generated successfully!");
        return 0;
    }
}