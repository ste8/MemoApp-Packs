namespace ImagesGenerator.Configuration;

public class OpenAISettings
{
    public const string SectionName = "OpenAI";
    
    public string ApiKey { get; set; } = string.Empty;
    
    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(ApiKey) 
               && ApiKey != "YOUR_API_KEY_HERE" 
               && ApiKey != "PUT_YOUR_ACTUAL_API_KEY_HERE"
               && ApiKey.StartsWith("sk-");  // OpenAI keys typically start with sk-
    }
}