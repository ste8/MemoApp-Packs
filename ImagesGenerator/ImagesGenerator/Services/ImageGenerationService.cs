using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI.Images;
using ImagesGenerator.Configuration;
using ImagesGenerator.Models;

namespace ImagesGenerator.Services;

public interface IImageGenerationService
{
    Task<byte[]?> GenerateImageAsync(Subject subject, CancellationToken cancellationToken = default);
}

public class ImageGenerationService : IImageGenerationService
{
    private readonly ImageClient _imageClient;
    private readonly IRetryPolicyService _retryPolicy;
    private readonly ILogger<ImageGenerationService> _logger;
    private readonly ImageGenerationSettings _settings;
    
    private const string PromptTemplate = @"Create lively cartoon-style illustrations of the following subject: ""{0}"" 

The image must be:
– Simple and easy to visualize mentally
– Highly distinctive and memorable
– Created with bright, contrasting, but realistic colors
– On a white background (RGB 255;255;255)
– Free of any text or numbers
- If the subject is not a human or an animal, don't add elements such as eyes, mouth, ...

– Square (1024x1024)

Focus on visual clarity, mnemonic effectiveness, and strong visual impact in a clean cartoon illustration style. No text.";
    
    public ImageGenerationService(
        ImageClient imageClient,
        IRetryPolicyService retryPolicy,
        IOptions<ImageGenerationSettings> settings,
        ILogger<ImageGenerationService> logger)
    {
        _imageClient = imageClient ?? throw new ArgumentNullException(nameof(imageClient));
        _retryPolicy = retryPolicy ?? throw new ArgumentNullException(nameof(retryPolicy));
        _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    public async Task<byte[]?> GenerateImageAsync(Subject subject, CancellationToken cancellationToken = default)
    {
        var prompt = string.Format(PromptTemplate, subject.Word.Trim());
        
        _logger.LogInformation("Starting image generation for {Subject}", subject);
        
        try
        {
            var result = await _retryPolicy.ExecuteAsync<byte[]>(
                async () => await GenerateImageCoreAsync(prompt, cancellationToken),
                subject.ToString(),
                cancellationToken);
            
            if (result != null)
            {
                _logger.LogInformation("Successfully generated image for {Subject}", subject);
            }
            else
            {
                _logger.LogWarning("Failed to generate image for {Subject} after all retries", subject);
            }
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating image for {Subject}", subject);
            throw;
        }
    }
    
    private async Task<byte[]?> GenerateImageCoreAsync(string prompt, CancellationToken cancellationToken)
    {
        var options = new ImageGenerationOptions
        {
            Size = ParseImageSize(_settings.Size),
            ResponseFormat = GeneratedImageFormat.Bytes,
            Quality = ParseImageQuality(_settings.Quality)
        };
        
        var response = await _imageClient.GenerateImageAsync(prompt, options, cancellationToken);
        return response.Value.ImageBytes?.ToArray();
    }
    
    private static GeneratedImageSize ParseImageSize(string size)
    {
        return size switch
        {
            "1024x1024" => GeneratedImageSize.W1024xH1024,
            "1024x1792" => GeneratedImageSize.W1024xH1792,
            "1792x1024" => GeneratedImageSize.W1792xH1024,
            _ => GeneratedImageSize.W1024xH1024
        };
    }
    
    private static GeneratedImageQuality ParseImageQuality(string quality)
    {
        return quality?.ToLower() switch
        {
            "hd" => GeneratedImageQuality.High,
            "standard" => GeneratedImageQuality.Standard,
            _ => GeneratedImageQuality.Standard
        };
    }
}