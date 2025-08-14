using System.Text.Json.Serialization;

namespace PacksBuilder.Models;

public class Manifest
{
    [JsonPropertyName("international_name")]
    public string InternationalName { get; set; } = string.Empty;

    [JsonPropertyName("native_name")]
    public string NativeName { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("native_description")]
    public string NativeDescription { get; set; } = string.Empty;

    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    [JsonPropertyName("language_code")]
    public string LanguageCode { get; set; } = string.Empty;

    [JsonPropertyName("author")]
    public string Author { get; set; } = string.Empty;
}