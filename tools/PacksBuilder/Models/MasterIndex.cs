using System.Text.Json.Serialization;

namespace PacksBuilder.Models;

public class MasterIndex
{
    [JsonPropertyName("last_updated")]
    public DateTime LastUpdated { get; set; }

    [JsonPropertyName("packs")]
    public List<PackEntry> Packs { get; set; } = new();
}

public class PackEntry
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

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

    [JsonPropertyName("file_size")]
    public long FileSize { get; set; }

    [JsonPropertyName("download_url")]
    public string DownloadUrl { get; set; } = string.Empty;
}