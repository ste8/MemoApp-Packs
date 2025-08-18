using System.IO.Compression;
using PacksBuilder.Configuration;
using PacksBuilder.Models;

namespace PacksBuilder.Services;

public class ZipService
{
    private readonly PackSettings _settings;
    private readonly FileDiscoveryService _fileDiscovery;

    public ZipService(PackSettings settings, FileDiscoveryService fileDiscovery)
    {
        _settings = settings;
        _fileDiscovery = fileDiscovery;
    }

    public (bool success, string zipPath, long fileSize) CreateZip(string packPath)
    {
        var manifest = _fileDiscovery.ReadManifest(packPath);
        if (manifest == null)
        {
            Console.WriteLine($"Error: No manifest.json found in {packPath}");
            return (false, string.Empty, 0);
        }

        var packName = Path.GetFileName(packPath);
        var zipFilename = GenerateZipFilename(manifest);
        var outputPath = Path.Combine(_settings.OutputDirectory, zipFilename);

        if (!Directory.Exists(_settings.OutputDirectory))
        {
            Directory.CreateDirectory(_settings.OutputDirectory);
        }

        if (File.Exists(outputPath))
        {
            var fileInfo = new FileInfo(outputPath);
            Console.WriteLine($"Zip already exists: {zipFilename} ({fileInfo.Length} bytes)");
            return (true, outputPath, fileInfo.Length);
        }

        try
        {
            CreateZipFromDirectory(packPath, outputPath);
            var createdFileInfo = new FileInfo(outputPath);
            Console.WriteLine($"Created zip: {zipFilename} ({createdFileInfo.Length} bytes)");
            return (true, outputPath, createdFileInfo.Length);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating zip for {packName}: {ex.Message}");
            return (false, string.Empty, 0);
        }
    }

    private string GenerateZipFilename(Manifest manifest)
    {
        var safeName = manifest.InternationalName.ToLowerInvariant()
            .Replace(" ", "_")
            .Replace("-", "_");
        
        var safeVersion = manifest.Version.Replace(" ", "_");
        
        return $"{safeName}_{safeVersion}.zip";
    }

    private void CreateZipFromDirectory(string sourceDir, string destinationZip)
    {
        if (File.Exists(destinationZip))
            File.Delete(destinationZip);

        using var archive = ZipFile.Open(destinationZip, ZipArchiveMode.Create);
        
        var directoryName = Path.GetFileName(sourceDir);
        AddDirectoryToZip(archive, sourceDir, directoryName);
    }

    private void AddDirectoryToZip(ZipArchive archive, string sourceDir, string entryName)
    {
        var files = Directory.GetFiles(sourceDir);
        foreach (var file in files)
        {
            var relativePath = Path.Combine(entryName, Path.GetFileName(file));
            archive.CreateEntryFromFile(file, relativePath, CompressionLevel.Optimal);
        }

        var subdirectories = Directory.GetDirectories(sourceDir);
        foreach (var subdirectory in subdirectories)
        {
            var subdirName = Path.GetFileName(subdirectory);
            var newEntryName = Path.Combine(entryName, subdirName);
            AddDirectoryToZip(archive, subdirectory, newEntryName);
        }
    }
}