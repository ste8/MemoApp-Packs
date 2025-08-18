using System.Text.Json.Serialization;

namespace PacksBuilder.Models;

public class Defaults
{
    [JsonPropertyName("numbers")]
    public Dictionary<string, string> Numbers { get; set; } = new();

    [JsonPropertyName("letters_upper")]
    public Dictionary<string, string> LettersUpper { get; set; } = new();

    [JsonPropertyName("letters_lower")]
    public Dictionary<string, string> LettersLower { get; set; } = new();

    [JsonPropertyName("symbols")]
    public Dictionary<string, string> Symbols { get; set; } = new();

    [JsonPropertyName("months")]
    public Dictionary<string, string> Months { get; set; } = new();

    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }
}