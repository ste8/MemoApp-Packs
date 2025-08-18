namespace ImagesGenerator.Models;

public class ImageGenerationResult
{
    public Subject Subject { get; }
    public bool Success { get; }
    public string? FilePath { get; }
    public string? ErrorMessage { get; }
    
    private ImageGenerationResult(Subject subject, bool success, string? filePath = null, string? errorMessage = null)
    {
        Subject = subject;
        Success = success;
        FilePath = filePath;
        ErrorMessage = errorMessage;
    }
    
    public static ImageGenerationResult CreateSuccess(Subject subject, string filePath)
        => new(subject, true, filePath);
    
    public static ImageGenerationResult CreateFailure(Subject subject, string errorMessage)
        => new(subject, false, errorMessage: errorMessage);
}