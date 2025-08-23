using System.Text.Json;
using PacksBuilder.Models;

namespace PacksBuilder.Services;

public class FileDiscoveryService
{
    private static readonly string[] ImageExtensions = { ".png", ".jpg", ".jpeg", ".webp" };

    public List<string> DiscoverPackFolders(string rootPath)
    {
        if (!Directory.Exists(rootPath))
            throw new DirectoryNotFoundException($"Root directory not found: {rootPath}");

        return Directory.GetDirectories(rootPath)
            .Where(dir => File.Exists(Path.Combine(dir, "manifest.json")))
            .ToList();
    }

    public Manifest? ReadManifest(string packPath)
    {
        var manifestPath = Path.Combine(packPath, "manifest.json");
        if (!File.Exists(manifestPath))
            return null;

        try
        {
            var json = File.ReadAllText(manifestPath);
            return JsonSerializer.Deserialize<Manifest>(json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading manifest from {manifestPath}: {ex.Message}");
            return null;
        }
    }

    public Dictionary<string, List<string>> DiscoverImages(string packPath)
    {
        var result = new Dictionary<string, List<string>>();
        var imagesPath = Path.Combine(packPath, "major_system", "images");

        if (!Directory.Exists(imagesPath))
            return result;

        var categories = Directory.GetDirectories(imagesPath);
        foreach (var categoryPath in categories)
        {
            var categoryName = Path.GetFileName(categoryPath);
            var imageFiles = Directory.GetFiles(categoryPath)
                .Where(file => ImageExtensions.Contains(Path.GetExtension(file).ToLowerInvariant()))
                .Select(Path.GetFileName)
                .Where(name => !string.IsNullOrEmpty(name))
                .Cast<string>()
                .OrderBy(name => name)
                .ToList();

            if (imageFiles.Any())
            {
                result[categoryName] = imageFiles;
            }
        }

        return result;
    }

    public string? ExtractTokenFromFilename(string filename)
    {
        if (string.IsNullOrWhiteSpace(filename))
            return null;

        var nameWithoutExtension = Path.GetFileNameWithoutExtension(filename);
        var underscoreIndex = nameWithoutExtension.IndexOf('_');
        
        if (underscoreIndex > 0)
        {
            return nameWithoutExtension.Substring(0, underscoreIndex);
        }

        return null;
    }
}