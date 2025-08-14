using Microsoft.Extensions.Configuration;
using PacksBuilder.Configuration;
using PacksBuilder.Services;

namespace PacksBuilder;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("=================================");
        Console.WriteLine("        Packs Builder");
        Console.WriteLine("=================================");
        Console.WriteLine();

        var settings = LoadConfiguration(args);
        DisplayConfiguration(settings);

        var fileDiscovery = new FileDiscoveryService();
        var defaultsService = new DefaultsService(fileDiscovery);
        var zipService = new ZipService(settings, fileDiscovery);
        var masterIndexService = new MasterIndexService(settings, fileDiscovery);

        RunMainMenu(settings, fileDiscovery, defaultsService, zipService, masterIndexService);
    }

    static PackSettings LoadConfiguration(string[] args)
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .AddCommandLine(args, new Dictionary<string, string>
            {
                { "--base-url", "PackSettings:BaseDownloadUrl" },
                { "--output-dir", "PackSettings:OutputDirectory" }
            });

        var configuration = builder.Build();
        var settings = new PackSettings();
        configuration.GetSection("PackSettings").Bind(settings);

        return settings;
    }

    static void DisplayConfiguration(PackSettings settings)
    {
        Console.WriteLine("Current Configuration:");
        Console.WriteLine($"  Base Download URL: {settings.BaseDownloadUrl}");
        Console.WriteLine($"  Output Directory: {settings.OutputDirectory}");
        Console.WriteLine();
    }

    static void RunMainMenu(PackSettings settings, FileDiscoveryService fileDiscovery, 
        DefaultsService defaultsService, ZipService zipService, MasterIndexService masterIndexService)
    {
        while (true)
        {
            Console.WriteLine("Main Menu:");
            Console.WriteLine("1. Create zip files for all major system packs and update master index");
            Console.WriteLine("2. Initialize/update defaults.json for a specific pack");
            Console.WriteLine("3. Regenerate master index file (scan existing zips)");
            Console.WriteLine("4. Exit");
            Console.Write("\nSelect an option (1-4): ");

            var choice = Console.ReadLine();
            Console.WriteLine();

            switch (choice)
            {
                case "1":
                    CreateZipsAndUpdateIndex(fileDiscovery, zipService, masterIndexService);
                    break;
                case "2":
                    InitializeDefaults(fileDiscovery, defaultsService);
                    break;
                case "3":
                    RegenerateMasterIndex(masterIndexService);
                    break;
                case "4":
                    Console.WriteLine("Exiting...");
                    return;
                default:
                    Console.WriteLine("Invalid option. Please try again.");
                    break;
            }

            Console.WriteLine();
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
            Console.Clear();
            
            Console.WriteLine("=================================");
            Console.WriteLine("        Packs Builder");
            Console.WriteLine("=================================");
            Console.WriteLine();
            DisplayConfiguration(settings);
        }
    }

    static void CreateZipsAndUpdateIndex(FileDiscoveryService fileDiscovery, 
        ZipService zipService, MasterIndexService masterIndexService)
    {
        Console.Write("Enter the root folder path containing major system packs: ");
        var rootPath = Console.ReadLine()?.Trim();

        if (string.IsNullOrEmpty(rootPath))
        {
            Console.WriteLine("Error: Path cannot be empty.");
            return;
        }

        if (!Directory.Exists(rootPath))
        {
            Console.WriteLine($"Error: Directory not found: {rootPath}");
            return;
        }

        var packFolders = fileDiscovery.DiscoverPackFolders(rootPath);
        
        if (packFolders.Count == 0)
        {
            Console.WriteLine("No pack folders with manifest.json found.");
            return;
        }

        Console.WriteLine($"Found {packFolders.Count} pack(s). Processing...");
        Console.WriteLine();

        int successCount = 0;
        foreach (var packPath in packFolders)
        {
            var packName = Path.GetFileName(packPath);
            Console.WriteLine($"Processing: {packName}");
            
            var (success, zipPath, fileSize) = zipService.CreateZip(packPath);
            
            if (success)
            {
                masterIndexService.UpdateMasterIndex(packPath, zipPath, fileSize);
                successCount++;
            }
        }

        Console.WriteLine();
        Console.WriteLine($"Completed: {successCount}/{packFolders.Count} packs processed successfully.");
    }

    static void InitializeDefaults(FileDiscoveryService fileDiscovery, DefaultsService defaultsService)
    {
        Console.Write("Enter the path to the major system pack folder: ");
        var packPath = Console.ReadLine()?.Trim();

        if (string.IsNullOrEmpty(packPath))
        {
            Console.WriteLine("Error: Path cannot be empty.");
            return;
        }

        if (!Directory.Exists(packPath))
        {
            Console.WriteLine($"Error: Directory not found: {packPath}");
            return;
        }

        var manifest = fileDiscovery.ReadManifest(packPath);
        if (manifest == null)
        {
            Console.WriteLine("Error: No manifest.json found in the specified folder.");
            return;
        }

        Console.WriteLine($"Pack: {manifest.InternationalName} v{manifest.Version}");
        Console.WriteLine("Initializing/updating defaults.json...");
        Console.WriteLine();

        defaultsService.InitializeOrUpdateDefaults(packPath);
    }

    static void RegenerateMasterIndex(MasterIndexService masterIndexService)
    {
        Console.WriteLine("Regenerating master index from existing zip files...");
        masterIndexService.RegenerateMasterIndex();
    }
}
