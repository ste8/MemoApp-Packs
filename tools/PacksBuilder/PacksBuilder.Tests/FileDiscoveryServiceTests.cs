using PacksBuilder.Services;

namespace PacksBuilder.Tests;

public class FileDiscoveryServiceTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly FileDiscoveryService _service;

    public FileDiscoveryServiceTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"PacksBuilderTests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDirectory);
        _service = new FileDiscoveryService();
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
            Directory.Delete(_testDirectory, true);
    }

    [Fact]
    public void DiscoverPackFolders_FindsFoldersWithManifest()
    {
        var pack1Dir = Path.Combine(_testDirectory, "pack1");
        var pack2Dir = Path.Combine(_testDirectory, "pack2");
        var pack3Dir = Path.Combine(_testDirectory, "pack3");
        
        Directory.CreateDirectory(pack1Dir);
        Directory.CreateDirectory(pack2Dir);
        Directory.CreateDirectory(pack3Dir);
        
        File.WriteAllText(Path.Combine(pack1Dir, "manifest.json"), "{}");
        File.WriteAllText(Path.Combine(pack2Dir, "manifest.json"), "{}");

        var result = _service.DiscoverPackFolders(_testDirectory);

        Assert.Equal(2, result.Count);
        Assert.Contains(pack1Dir, result);
        Assert.Contains(pack2Dir, result);
        Assert.DoesNotContain(pack3Dir, result);
    }

    [Fact]
    public void DiscoverPackFolders_ThrowsIfDirectoryNotFound()
    {
        var nonExistentPath = Path.Combine(_testDirectory, "nonexistent");

        Assert.Throws<DirectoryNotFoundException>(() => 
            _service.DiscoverPackFolders(nonExistentPath));
    }

    [Fact]
    public void ReadManifest_ReturnsManifestObject()
    {
        var packDir = Path.Combine(_testDirectory, "pack");
        Directory.CreateDirectory(packDir);
        
        var manifestJson = @"{
            ""international_name"": ""Italian"",
            ""native_name"": ""Italiano"",
            ""description"": ""Test description"",
            ""native_description"": ""Descrizione test"",
            ""version"": ""1.0.0"",
            ""language_code"": ""it"",
            ""author"": ""Test Author""
        }";
        
        File.WriteAllText(Path.Combine(packDir, "manifest.json"), manifestJson);

        var result = _service.ReadManifest(packDir);

        Assert.NotNull(result);
        Assert.Equal("Italian", result.InternationalName);
        Assert.Equal("Italiano", result.NativeName);
        Assert.Equal("1.0.0", result.Version);
        Assert.Equal("it", result.LanguageCode);
    }

    [Fact]
    public void ReadManifest_ReturnsNullIfNoManifest()
    {
        var packDir = Path.Combine(_testDirectory, "pack");
        Directory.CreateDirectory(packDir);

        var result = _service.ReadManifest(packDir);

        Assert.Null(result);
    }

    [Fact]
    public void DiscoverImages_FindsImagesInCategories()
    {
        var packDir = Path.Combine(_testDirectory, "pack");
        var imagesDir = Path.Combine(packDir, "major_system", "images");
        var numbersDir = Path.Combine(imagesDir, "numbers");
        var lettersDir = Path.Combine(imagesDir, "letters_upper");
        
        Directory.CreateDirectory(numbersDir);
        Directory.CreateDirectory(lettersDir);
        
        File.WriteAllText(Path.Combine(numbersDir, "00_sasso.png"), "");
        File.WriteAllText(Path.Combine(numbersDir, "01_te.png"), "");
        File.WriteAllText(Path.Combine(numbersDir, "readme.txt"), "");
        File.WriteAllText(Path.Combine(lettersDir, "A_apple.jpg"), "");

        var result = _service.DiscoverImages(packDir);

        Assert.Equal(2, result.Count);
        Assert.Contains("numbers", result.Keys);
        Assert.Contains("letters_upper", result.Keys);
        Assert.Equal(2, result["numbers"].Count);
        Assert.Single(result["letters_upper"]);
        Assert.Contains("00_sasso.png", result["numbers"]);
        Assert.DoesNotContain("readme.txt", result["numbers"]);
    }

    [Fact]
    public void DiscoverImages_SupportsWebpFormat()
    {
        var packDir = Path.Combine(_testDirectory, "pack");
        var imagesDir = Path.Combine(packDir, "major_system", "images");
        var numbersDir = Path.Combine(imagesDir, "numbers");
        
        Directory.CreateDirectory(numbersDir);
        
        File.WriteAllText(Path.Combine(numbersDir, "00_sasso.webp"), "");
        File.WriteAllText(Path.Combine(numbersDir, "01_te.webp"), "");
        File.WriteAllText(Path.Combine(numbersDir, "02_mixed.png"), "");
        File.WriteAllText(Path.Combine(numbersDir, "readme.txt"), "");

        var result = _service.DiscoverImages(packDir);

        Assert.Single(result);
        Assert.Contains("numbers", result.Keys);
        Assert.Equal(3, result["numbers"].Count);
        Assert.Contains("00_sasso.webp", result["numbers"]);
        Assert.Contains("01_te.webp", result["numbers"]);
        Assert.Contains("02_mixed.png", result["numbers"]);
        Assert.DoesNotContain("readme.txt", result["numbers"]);
    }

    [Fact]
    public void DiscoverImages_ReturnsEmptyIfNoImagesDir()
    {
        var packDir = Path.Combine(_testDirectory, "pack");
        Directory.CreateDirectory(packDir);

        var result = _service.DiscoverImages(packDir);

        Assert.Empty(result);
    }

    [Fact]
    public void ExtractTokenFromFilename_ExtractsCorrectToken()
    {
        Assert.Equal("00", _service.ExtractTokenFromFilename("00_sasso.png"));
        Assert.Equal("01", _service.ExtractTokenFromFilename("01_te.png"));
        Assert.Equal("A", _service.ExtractTokenFromFilename("A_apple.jpg"));
        Assert.Equal("@", _service.ExtractTokenFromFilename("@_at.png"));
    }

    [Fact]
    public void ExtractTokenFromFilename_ReturnsNullForInvalidFilename()
    {
        Assert.Null(_service.ExtractTokenFromFilename(""));
        Assert.Null(_service.ExtractTokenFromFilename("noundercore.png"));
        Assert.Null(_service.ExtractTokenFromFilename("_startsWithUnderscore.png"));
    }
}