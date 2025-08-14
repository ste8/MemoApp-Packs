using System.IO.Compression;
using PacksBuilder.Configuration;
using PacksBuilder.Services;

namespace PacksBuilder.Tests;

public class ZipServiceTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly PackSettings _settings;
    private readonly FileDiscoveryService _fileDiscovery;
    private readonly ZipService _service;

    public ZipServiceTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"PacksBuilderTests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDirectory);
        
        _settings = new PackSettings
        {
            BaseDownloadUrl = "https://test.com/packs/",
            OutputDirectory = Path.Combine(_testDirectory, "output")
        };
        
        _fileDiscovery = new FileDiscoveryService();
        _service = new ZipService(_settings, _fileDiscovery);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
            Directory.Delete(_testDirectory, true);
    }

    [Fact]
    public void CreateZip_CreatesZipWithCorrectName()
    {
        var packDir = SetupTestPack();

        var (success, zipPath, fileSize) = _service.CreateZip(packDir);

        Assert.True(success);
        Assert.Contains("italian_1.0.0.zip", zipPath);
        Assert.True(File.Exists(zipPath));
        Assert.True(fileSize > 0);
    }

    [Fact]
    public void CreateZip_IncludesAllFilesAndFolders()
    {
        var packDir = SetupTestPackWithStructure();

        var (success, zipPath, _) = _service.CreateZip(packDir);

        Assert.True(success);
        
        using var archive = ZipFile.OpenRead(zipPath);
        var entries = archive.Entries.Select(e => e.FullName).ToList();
        
        Assert.Contains(entries, e => e.Contains("manifest.json"));
        Assert.Contains(entries, e => e.Contains("defaults.json"));
        Assert.Contains(entries, e => e.Contains("images/numbers/00_sasso.png"));
        Assert.Contains(entries, e => e.Contains("thumbnails/numbers/00_sasso_thumb.png"));
    }

    [Fact]
    public void CreateZip_SkipsIfZipAlreadyExists()
    {
        var packDir = SetupTestPack();
        Directory.CreateDirectory(_settings.OutputDirectory);
        
        var expectedZipPath = Path.Combine(_settings.OutputDirectory, "italian_1.0.0.zip");
        File.WriteAllText(expectedZipPath, "existing content");
        var originalSize = new FileInfo(expectedZipPath).Length;

        var (success, zipPath, fileSize) = _service.CreateZip(packDir);

        Assert.True(success);
        Assert.Equal(expectedZipPath, zipPath);
        Assert.Equal(originalSize, fileSize);
        
        var content = File.ReadAllText(expectedZipPath);
        Assert.Equal("existing content", content);
    }

    [Fact]
    public void CreateZip_FailsIfNoManifest()
    {
        var packDir = Path.Combine(_testDirectory, "pack");
        Directory.CreateDirectory(packDir);

        var (success, zipPath, fileSize) = _service.CreateZip(packDir);

        Assert.False(success);
        Assert.Equal(string.Empty, zipPath);
        Assert.Equal(0, fileSize);
    }

    [Fact]
    public void CreateZip_CreatesOutputDirectoryIfNotExists()
    {
        var packDir = SetupTestPack();
        
        Assert.False(Directory.Exists(_settings.OutputDirectory));

        var (success, _, _) = _service.CreateZip(packDir);

        Assert.True(success);
        Assert.True(Directory.Exists(_settings.OutputDirectory));
    }

    [Fact]
    public void GenerateZipFilename_HandlesSpecialCharacters()
    {
        var packDir = SetupTestPack("Italian Company-X", "1.0.0-beta");

        var (success, zipPath, _) = _service.CreateZip(packDir);

        Assert.True(success);
        Assert.Contains("italian_company_x_1.0.0-beta.zip", zipPath);
    }

    private string SetupTestPack(string internationalName = "Italian", string version = "1.0.0")
    {
        var packDir = Path.Combine(_testDirectory, "pack");
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

    private string SetupTestPackWithStructure()
    {
        var packDir = SetupTestPack();
        
        File.WriteAllText(Path.Combine(packDir, "defaults.json"), "{}");
        
        var imagesNumbersDir = Path.Combine(packDir, "images", "numbers");
        Directory.CreateDirectory(imagesNumbersDir);
        File.WriteAllText(Path.Combine(imagesNumbersDir, "00_sasso.png"), "image content");
        
        var thumbnailsNumbersDir = Path.Combine(packDir, "thumbnails", "numbers");
        Directory.CreateDirectory(thumbnailsNumbersDir);
        File.WriteAllText(Path.Combine(thumbnailsNumbersDir, "00_sasso_thumb.png"), "thumb content");
        
        return packDir;
    }
}