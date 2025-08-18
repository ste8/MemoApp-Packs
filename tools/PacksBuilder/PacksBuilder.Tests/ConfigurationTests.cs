using Microsoft.Extensions.Configuration;
using PacksBuilder.Configuration;

namespace PacksBuilder.Tests;

public class ConfigurationTests
{
    [Fact]
    public void PackSettings_HasDefaultValues()
    {
        var settings = new PackSettings();

        Assert.Equal("https://yourserver.com/packs/", settings.BaseDownloadUrl);
        Assert.Equal("./output", settings.OutputDirectory);
    }

    [Fact]
    public void Configuration_LoadsFromAppSettings()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"ConfigTest_{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);
        
        try
        {
            var appSettingsPath = Path.Combine(tempDir, "appsettings.json");
            var appSettingsContent = @"{
                ""PackSettings"": {
                    ""BaseDownloadUrl"": ""https://custom.com/downloads/"",
                    ""OutputDirectory"": ""./custom-output""
                }
            }";
            File.WriteAllText(appSettingsPath, appSettingsContent);

            var builder = new ConfigurationBuilder()
                .SetBasePath(tempDir)
                .AddJsonFile("appsettings.json", optional: true);

            var configuration = builder.Build();
            var settings = new PackSettings();
            configuration.GetSection("PackSettings").Bind(settings);

            Assert.Equal("https://custom.com/downloads/", settings.BaseDownloadUrl);
            Assert.Equal("./custom-output", settings.OutputDirectory);
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void Configuration_CommandLineOverridesAppSettings()
    {
        var args = new[]
        {
            "--base-url", "https://cli.com/packs/",
            "--output-dir", "./cli-output"
        };

        var builder = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "PackSettings:BaseDownloadUrl", "https://default.com/" },
                { "PackSettings:OutputDirectory", "./default" }
            })
            .AddCommandLine(args, new Dictionary<string, string>
            {
                { "--base-url", "PackSettings:BaseDownloadUrl" },
                { "--output-dir", "PackSettings:OutputDirectory" }
            });

        var configuration = builder.Build();
        var settings = new PackSettings();
        configuration.GetSection("PackSettings").Bind(settings);

        Assert.Equal("https://cli.com/packs/", settings.BaseDownloadUrl);
        Assert.Equal("./cli-output", settings.OutputDirectory);
    }
}