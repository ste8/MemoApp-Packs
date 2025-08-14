using System.Text.Json;
using PacksBuilder.Services;

namespace PacksBuilder.Tests;

public class DefaultsServiceTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly FileDiscoveryService _fileDiscovery;
    private readonly DefaultsService _service;

    public DefaultsServiceTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"PacksBuilderTests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDirectory);
        _fileDiscovery = new FileDiscoveryService();
        _service = new DefaultsService(_fileDiscovery);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
            Directory.Delete(_testDirectory, true);
    }

    [Fact]
    public void InitializeOrUpdateDefaults_CreatesNewDefaultsFile()
    {
        var packDir = SetupTestPack();
        var defaultsPath = Path.Combine(packDir, "defaults.json");

        _service.InitializeOrUpdateDefaults(packDir);

        Assert.True(File.Exists(defaultsPath));
        
        var json = File.ReadAllText(defaultsPath);
        var defaults = JsonDocument.Parse(json);
        
        Assert.True(defaults.RootElement.TryGetProperty("numbers", out var numbers));
        Assert.True(numbers.TryGetProperty("00", out var token00));
        Assert.Equal("00_sasso.png", token00.GetString());
    }

    [Fact]
    public void InitializeOrUpdateDefaults_MergesWithExistingDefaults()
    {
        var packDir = SetupTestPack();
        var defaultsPath = Path.Combine(packDir, "defaults.json");
        
        var existingDefaults = @"{
            ""numbers"": {
                ""00"": ""00_custom.png"",
                ""02"": ""02_existing.png""
            }
        }";
        File.WriteAllText(defaultsPath, existingDefaults);

        _service.InitializeOrUpdateDefaults(packDir);

        var json = File.ReadAllText(defaultsPath);
        var defaults = JsonDocument.Parse(json);
        
        var numbers = defaults.RootElement.GetProperty("numbers");
        
        Assert.Equal("00_custom.png", numbers.GetProperty("00").GetString());
        
        Assert.Equal("02_existing.png", numbers.GetProperty("02").GetString());
        
        Assert.True(numbers.TryGetProperty("01", out var token01));
        Assert.Equal("01_te.png", token01.GetString());
    }

    [Fact]
    public void InitializeOrUpdateDefaults_AddsNewCategory()
    {
        var packDir = SetupTestPack();
        var defaultsPath = Path.Combine(packDir, "defaults.json");
        
        var existingDefaults = @"{
            ""numbers"": {
                ""00"": ""00_sasso.png""
            }
        }";
        File.WriteAllText(defaultsPath, existingDefaults);

        var lettersDir = Path.Combine(packDir, "images", "letters_upper");
        Directory.CreateDirectory(lettersDir);
        File.WriteAllText(Path.Combine(lettersDir, "A_apple.png"), "");

        _service.InitializeOrUpdateDefaults(packDir);

        var json = File.ReadAllText(defaultsPath);
        var defaults = JsonDocument.Parse(json);
        
        Assert.True(defaults.RootElement.TryGetProperty("letters_upper", out var letters));
        Assert.True(letters.TryGetProperty("A", out var tokenA));
        Assert.Equal("A_apple.png", tokenA.GetString());
    }

    [Fact]
    public void InitializeOrUpdateDefaults_HandlesEmptyImageDirectory()
    {
        var packDir = Path.Combine(_testDirectory, "pack");
        Directory.CreateDirectory(Path.Combine(packDir, "images"));

        _service.InitializeOrUpdateDefaults(packDir);

        Assert.False(File.Exists(Path.Combine(packDir, "defaults.json")));
    }

    private string SetupTestPack()
    {
        var packDir = Path.Combine(_testDirectory, "pack");
        var imagesDir = Path.Combine(packDir, "images");
        var numbersDir = Path.Combine(imagesDir, "numbers");
        
        Directory.CreateDirectory(numbersDir);
        
        File.WriteAllText(Path.Combine(numbersDir, "00_sasso.png"), "");
        File.WriteAllText(Path.Combine(numbersDir, "01_te.png"), "");
        
        return packDir;
    }
}