namespace PacksBuilder.Configuration;

public class PackSettings
{
    public string BaseDownloadUrl { get; set; } = "https://yourserver.com/packs/";
    public string OutputDirectory { get; set; } = "./output";
}