using System.Text.Json;
using PacksBuilder.Configuration;
using PacksBuilder.Services;

namespace PacksBuilder.Tests;

public class MasterIndexServiceTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly PackSettings _settings;
    private readonly FileDiscoveryService _fileDiscovery;
    private readonly MasterIndexService _service;

    public MasterIndexServiceTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"PacksBuilderTests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDirectory);
        
        _settings = new PackSettings
        {
            BaseDownloadUrl = "https://test.com/packs/",
            OutputDirectory = Path.Combine(_testDirectory, "output")
        };
        
        _fileDiscovery = new FileDiscoveryService();
        _service = new MasterIndexService(_settings, _fileDiscovery);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
            Directory.Delete(_testDirectory, true);
    }

    [Fact]
    public void UpdateMasterIndex_CreatesNewIndex()
    {
        var packDir = SetupTestPack();
        var zipPath = Path.Combine(_settings.OutputDirectory, "italian_1.0.0.zip");
        Directory.CreateDirectory(_settings.OutputDirectory);
        File.WriteAllText(zipPath, "fake zip content");

        _service.UpdateMasterIndex(packDir, zipPath, 1000);

        var indexPath = Path.Combine(_settings.OutputDirectory, "major_system_packs.json");
        Assert.True(File.Exists(indexPath));

        var json = File.ReadAllText(indexPath);
        var index = JsonDocument.Parse(json);
        
        var packs = index.RootElement.GetProperty("packs");
        Assert.Equal(1, packs.GetArrayLength());
        
        var pack = packs[0];
        Assert.Equal("Italian", pack.GetProperty("international_name").GetString());
        Assert.Equal("1.0.0", pack.GetProperty("version").GetString());
        Assert.Equal("italian_1.0.0.zip", pack.GetProperty("filename").GetString());
        Assert.Equal(1000, pack.GetProperty("file_size").GetInt64());
        Assert.Equal("https://test.com/packs/italian_1.0.0.zip", pack.GetProperty("download_url").GetString());
    }

    [Fact]
    public void UpdateMasterIndex_UpdatesExistingEntry()
    {
        var packDir = SetupTestPack();
        Directory.CreateDirectory(_settings.OutputDirectory);
        
        var existingIndex = @"{
            ""last_updated"": ""2025-01-01T00:00:00Z"",
            ""packs"": [
                {
                    ""international_name"": ""Italian"",
                    ""native_name"": ""Old Name"",
                    ""description"": ""Old Description"",
                    ""native_description"": ""Old Native Description"",
                    ""version"": ""1.0.0"",
                    ""language_code"": ""it"",
                    ""author"": ""Old Author"",
                    ""filename"": ""old.zip"",
                    ""file_size"": 500,
                    ""download_url"": ""https://old.com/old.zip""
                }
            ]
        }";
        
        var indexPath = Path.Combine(_settings.OutputDirectory, "major_system_packs.json");
        File.WriteAllText(indexPath, existingIndex);

        var zipPath = Path.Combine(_settings.OutputDirectory, "italian_1.0.0.zip");
        _service.UpdateMasterIndex(packDir, zipPath, 2000);

        var json = File.ReadAllText(indexPath);
        var index = JsonDocument.Parse(json);
        
        var packs = index.RootElement.GetProperty("packs");
        Assert.Equal(1, packs.GetArrayLength());
        
        var pack = packs[0];
        Assert.Equal("Italiano", pack.GetProperty("native_name").GetString());
        Assert.Equal("italian_1.0.0.zip", pack.GetProperty("filename").GetString());
        Assert.Equal(2000, pack.GetProperty("file_size").GetInt64());
    }

    [Fact]
    public void UpdateMasterIndex_SortsPacksByInternationalName()
    {
        Directory.CreateDirectory(_settings.OutputDirectory);
        
        var pack1Dir = SetupTestPack("Spanish", "2.0.0");
        _service.UpdateMasterIndex(pack1Dir, "spanish.zip", 1000);
        
        var pack2Dir = SetupTestPack("Italian", "1.0.0");
        _service.UpdateMasterIndex(pack2Dir, "italian.zip", 2000);
        
        var pack3Dir = SetupTestPack("English", "3.0.0");
        _service.UpdateMasterIndex(pack3Dir, "english.zip", 3000);

        var indexPath = Path.Combine(_settings.OutputDirectory, "major_system_packs.json");
        var json = File.ReadAllText(indexPath);
        var index = JsonDocument.Parse(json);
        
        var packs = index.RootElement.GetProperty("packs");
        Assert.Equal(3, packs.GetArrayLength());
        
        Assert.Equal("English", packs[0].GetProperty("international_name").GetString());
        Assert.Equal("Italian", packs[1].GetProperty("international_name").GetString());
        Assert.Equal("Spanish", packs[2].GetProperty("international_name").GetString());
    }

    [Fact]
    public void RegenerateMasterIndex_ScansExistingZips()
    {
        Directory.CreateDirectory(_settings.OutputDirectory);
        
        File.WriteAllText(Path.Combine(_settings.OutputDirectory, "italian_1.0.0.zip"), "content1");
        File.WriteAllText(Path.Combine(_settings.OutputDirectory, "spanish_2.0.0.zip"), "content2");
        File.WriteAllText(Path.Combine(_settings.OutputDirectory, "notapack.txt"), "ignore");

        _service.RegenerateMasterIndex();

        var indexPath = Path.Combine(_settings.OutputDirectory, "major_system_packs.json");
        Assert.True(File.Exists(indexPath));

        var json = File.ReadAllText(indexPath);
        var index = JsonDocument.Parse(json);
        
        var packs = index.RootElement.GetProperty("packs");
        Assert.Equal(2, packs.GetArrayLength());
    }

    private string SetupTestPack(string internationalName = "Italian", string version = "1.0.0")
    {
        var packDir = Path.Combine(_testDirectory, $"pack_{Guid.NewGuid()}");
        Directory.CreateDirectory(packDir);
        
        var manifest = $@"{{
            ""international_name"": ""{internationalName}"",
            ""native_name"": ""Italiano"",
            ""description"": ""Test description"",
            ""native_description"": ""Descrizione test"",
            ""version"": ""{version}"",
            ""language_code"": ""it"",
            ""author"": ""Test Author""
        }}";
        
        File.WriteAllText(Path.Combine(packDir, "manifest.json"), manifest);
        
        return packDir;
    }
}