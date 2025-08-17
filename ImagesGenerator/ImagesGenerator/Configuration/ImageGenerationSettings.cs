namespace ImagesGenerator.Configuration;

public class ImageGenerationSettings
{
    public const string SectionName = "ImageGeneration";
    
    public string Model { get; set; } = "dall-e-3";
    public string Size { get; set; } = "1024x1024";
    public string Quality { get; set; } = "standard";
    public string OutputDirectory { get; set; } = "out";
    public int MaxRetries { get; set; } = 3;
    public int BaseDelaySeconds { get; set; } = 1;
}