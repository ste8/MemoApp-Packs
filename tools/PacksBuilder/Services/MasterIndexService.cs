using System.Text.Json;
using PacksBuilder.Configuration;
using PacksBuilder.Models;

namespace PacksBuilder.Services;

public class MasterIndexService
{
    private readonly PackSettings _settings;
    private readonly FileDiscoveryService _fileDiscovery;
    private readonly JsonSerializerOptions _jsonOptions;

    public MasterIndexService(PackSettings settings, FileDiscoveryService fileDiscovery)
    {
        _settings = settings;
        _fileDiscovery = fileDiscovery;
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        };
    }

    public void UpdateMasterIndex(string packPath, string zipPath, long fileSize)
    {
        var manifest = _fileDiscovery.ReadManifest(packPath);
        if (manifest == null)
            return;

        var indexPath = Path.Combine(_settings.OutputDirectory, "packs.json");
        var masterIndex = LoadMasterIndex(indexPath);

        var filename = Path.GetFileName(zipPath);
        var downloadUrl = BuildDownloadUrl(filename);

        var existingEntry = masterIndex.Packs.FirstOrDefault(p => 
            p.InternationalName == manifest.InternationalName && 
            p.Version == manifest.Version);

        if (existingEntry != null)
        {
            existingEntry.NativeName = manifest.NativeName;
            existingEntry.Description = manifest.Description;
            existingEntry.NativeDescription = manifest.NativeDescription;
            existingEntry.LanguageCode = manifest.LanguageCode;
            existingEntry.Author = manifest.Author;
            existingEntry.Filename = filename;
            existingEntry.FileSize = fileSize;
            existingEntry.DownloadUrl = downloadUrl;
        }
        else
        {
            masterIndex.Packs.Add(new PackEntry
            {
                InternationalName = manifest.InternationalName,
                NativeName = manifest.NativeName,
                Description = manifest.Description,
                NativeDescription = manifest.NativeDescription,
                Version = manifest.Version,
                LanguageCode = manifest.LanguageCode,
                Author = manifest.Author,
                Filename = filename,
                FileSize = fileSize,
                DownloadUrl = downloadUrl
            });
        }

        masterIndex.LastUpdated = DateTime.UtcNow;
        masterIndex.Packs = masterIndex.Packs
            .OrderBy(p => p.InternationalName)
            .ToList();

        SaveMasterIndex(indexPath, masterIndex);
    }

    public void RegenerateMasterIndex()
    {
        if (!Directory.Exists(_settings.OutputDirectory))
        {
            Console.WriteLine("Output directory does not exist. No zips to scan.");
            return;
        }

        var indexPath = Path.Combine(_settings.OutputDirectory, "packs.json");
        var masterIndex = new MasterIndex
        {
            LastUpdated = DateTime.UtcNow,
            Packs = new List<PackEntry>()
        };

        var zipFiles = Directory.GetFiles(_settings.OutputDirectory, "*.zip");
        
        foreach (var zipFile in zipFiles)
        {
            var fileInfo = new FileInfo(zipFile);
            var filename = Path.GetFileName(zipFile);
            
            var packName = ExtractPackNameFromZipFilename(filename);
            var version = ExtractVersionFromZipFilename(filename);

            if (!string.IsNullOrEmpty(packName) && !string.IsNullOrEmpty(version))
            {
                masterIndex.Packs.Add(new PackEntry
                {
                    InternationalName = packName,
                    NativeName = packName,
                    Description = $"Major system pack for {packName}",
                    NativeDescription = $"Major system pack for {packName}",
                    Version = version,
                    LanguageCode = "",
                    Author = "",
                    Filename = filename,
                    FileSize = fileInfo.Length,
                    DownloadUrl = BuildDownloadUrl(filename)
                });
            }
        }

        masterIndex.Packs = masterIndex.Packs
            .OrderBy(p => p.InternationalName)
            .ToList();

        SaveMasterIndex(indexPath, masterIndex);
        Console.WriteLine($"Regenerated master index with {masterIndex.Packs.Count} pack(s).");
    }

    private MasterIndex LoadMasterIndex(string indexPath)
    {
        if (!File.Exists(indexPath))
            return new MasterIndex { LastUpdated = DateTime.UtcNow };

        try
        {
            var json = File.ReadAllText(indexPath);
            return JsonSerializer.Deserialize<MasterIndex>(json, _jsonOptions) 
                ?? new MasterIndex { LastUpdated = DateTime.UtcNow };
        }
        catch
        {
            return new MasterIndex { LastUpdated = DateTime.UtcNow };
        }
    }

    private void SaveMasterIndex(string indexPath, MasterIndex masterIndex)
    {
        if (!Directory.Exists(_settings.OutputDirectory))
        {
            Directory.CreateDirectory(_settings.OutputDirectory);
        }

        var json = JsonSerializer.Serialize(masterIndex, _jsonOptions);
        File.WriteAllText(indexPath, json);
    }

    private string BuildDownloadUrl(string filename)
    {
        var baseUrl = _settings.BaseDownloadUrl.TrimEnd('/');
        return $"{baseUrl}/{filename}";
    }

    private string ExtractPackNameFromZipFilename(string filename)
    {
        var nameWithoutExt = Path.GetFileNameWithoutExtension(filename);
        var lastUnderscore = nameWithoutExt.LastIndexOf('_');
        
        if (lastUnderscore > 0)
        {
            return nameWithoutExt.Substring(0, lastUnderscore)
                .Replace("_", " ")
                .ToTitleCase();
        }

        return nameWithoutExt.Replace("_", " ").ToTitleCase();
    }

    private string ExtractVersionFromZipFilename(string filename)
    {
        var nameWithoutExt = Path.GetFileNameWithoutExtension(filename);
        var lastUnderscore = nameWithoutExt.LastIndexOf('_');
        
        if (lastUnderscore > 0 && lastUnderscore < nameWithoutExt.Length - 1)
        {
            return nameWithoutExt.Substring(lastUnderscore + 1);
        }

        return "1.0.0";
    }
}

public static class StringExtensions
{
    public static string ToTitleCase(this string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return text;

        var words = text.Split(' ');
        for (int i = 0; i < words.Length; i++)
        {
            if (words[i].Length > 0)
            {
                words[i] = char.ToUpper(words[i][0]) + words[i].Substring(1).ToLower();
            }
        }
        return string.Join(" ", words);
    }
}