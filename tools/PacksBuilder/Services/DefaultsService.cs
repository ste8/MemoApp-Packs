using System.Text.Json;
using System.Text.Json.Nodes;

namespace PacksBuilder.Services;

public class DefaultsService
{
    private readonly FileDiscoveryService _fileDiscovery;
    private readonly JsonSerializerOptions _jsonOptions;

    public DefaultsService(FileDiscoveryService fileDiscovery)
    {
        _fileDiscovery = fileDiscovery;
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        };
    }

    public void InitializeOrUpdateDefaults(string packPath)
    {
        var defaultsPath = Path.Combine(packPath, "defaults.json");
        var imagesByCategory = _fileDiscovery.DiscoverImages(packPath);

        if (imagesByCategory.Count == 0)
        {
            Console.WriteLine("No images found in pack.");
            return;
        }

        var existingDefaults = LoadExistingDefaults(defaultsPath);
        var mergedDefaults = MergeDefaults(existingDefaults, imagesByCategory);

        SaveDefaults(defaultsPath, mergedDefaults);
        ReportChanges(existingDefaults, mergedDefaults);
    }

    private JsonObject LoadExistingDefaults(string defaultsPath)
    {
        if (!File.Exists(defaultsPath))
            return new JsonObject();

        try
        {
            var json = File.ReadAllText(defaultsPath);
            return JsonNode.Parse(json)?.AsObject() ?? new JsonObject();
        }
        catch
        {
            return new JsonObject();
        }
    }

    private JsonObject MergeDefaults(JsonObject existing, Dictionary<string, List<string>> imagesByCategory)
    {
        var merged = JsonNode.Parse(existing.ToJsonString())?.AsObject() ?? new JsonObject();

        foreach (var (category, images) in imagesByCategory)
        {
            var categoryKey = NormalizeCategoryName(category);
            
            if (!merged.ContainsKey(categoryKey))
            {
                merged[categoryKey] = new JsonObject();
            }

            var categoryDefaults = merged[categoryKey]?.AsObject() ?? new JsonObject();
            
            var tokenGroups = images
                .Select(img => new { Token = _fileDiscovery.ExtractTokenFromFilename(img), Image = img })
                .Where(x => !string.IsNullOrEmpty(x.Token))
                .GroupBy(x => x.Token!)
                .ToList();

            foreach (var group in tokenGroups)
            {
                var token = group.Key;
                
                if (!categoryDefaults.ContainsKey(token))
                {
                    categoryDefaults[token] = group.First().Image;
                }
            }

            merged[categoryKey] = categoryDefaults;
        }

        return merged;
    }

    private string NormalizeCategoryName(string category)
    {
        return category.ToLowerInvariant().Replace("-", "_").Replace(" ", "_");
    }

    private void SaveDefaults(string defaultsPath, JsonObject defaults)
    {
        var json = defaults.ToJsonString(_jsonOptions);
        File.WriteAllText(defaultsPath, json);
    }

    private void ReportChanges(JsonObject oldDefaults, JsonObject newDefaults)
    {
        var addedCategories = new List<string>();
        var addedTokens = new Dictionary<string, List<string>>();

        foreach (var category in newDefaults)
        {
            if (!oldDefaults.ContainsKey(category.Key))
            {
                addedCategories.Add(category.Key);
            }
            else
            {
                var oldCategory = oldDefaults[category.Key]?.AsObject() ?? new JsonObject();
                var newCategory = category.Value?.AsObject() ?? new JsonObject();
                
                var newTokens = newCategory
                    .Where(token => !oldCategory.ContainsKey(token.Key))
                    .Select(token => token.Key)
                    .ToList();

                if (newTokens.Any())
                {
                    addedTokens[category.Key] = newTokens;
                }
            }
        }

        if (addedCategories.Any())
        {
            Console.WriteLine($"Added categories: {string.Join(", ", addedCategories)}");
        }

        foreach (var (category, tokens) in addedTokens)
        {
            Console.WriteLine($"Added {tokens.Count} new token(s) to category '{category}'");
        }

        if (!addedCategories.Any() && !addedTokens.Any())
        {
            Console.WriteLine("No changes made to defaults.json");
        }
        else
        {
            Console.WriteLine("defaults.json updated successfully.");
        }
    }
}