using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI.Images;
using ImagesGenerator.Configuration;
using ImagesGenerator.Services;

var builder = Host.CreateApplicationBuilder(args);

// Explicitly configure the configuration sources
builder.Configuration.Sources.Clear();
builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

// Configure services
builder.Services.Configure<OpenAISettings>(
    builder.Configuration.GetSection(OpenAISettings.SectionName));
builder.Services.Configure<ImageGenerationSettings>(
    builder.Configuration.GetSection(ImageGenerationSettings.SectionName));

// Register services
builder.Services.AddSingleton<ISubjectService, SubjectService>();
builder.Services.AddSingleton<IFileService, FileService>();
builder.Services.AddSingleton<IRetryPolicyService, RetryPolicyService>();
builder.Services.AddSingleton<IImageGenerationService, ImageGenerationService>();
builder.Services.AddSingleton<IImageProcessingOrchestrator, ImageProcessingOrchestrator>();

// Register OpenAI ImageClient
builder.Services.AddSingleton<ImageClient>(serviceProvider =>
{
    var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
    var openAISettings = serviceProvider.GetRequiredService<IOptions<OpenAISettings>>().Value;
    var imageSettings = serviceProvider.GetRequiredService<IOptions<ImageGenerationSettings>>().Value;
    
    logger.LogDebug("Environment: {Environment}", builder.Environment.EnvironmentName);
    logger.LogDebug("API Key configured: {IsConfigured}", !string.IsNullOrWhiteSpace(openAISettings.ApiKey));
    
    if (!openAISettings.IsValid())
    {
        logger.LogError("OpenAI API key is not valid. Current value: {ApiKey}", 
            string.IsNullOrWhiteSpace(openAISettings.ApiKey) ? "[empty]" : "[placeholder]");
        throw new InvalidOperationException(
            "OpenAI API key is not configured. Please set your API key in appsettings.Development.json, " +
            "environment variable OPENAI:ApiKey, or appsettings.json");
    }
    
    logger.LogInformation("OpenAI client configured with model: {Model}", imageSettings.Model);
    return new ImageClient(imageSettings.Model, openAISettings.ApiKey);
});

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Debug);

var host = builder.Build();

// Run the application
try
{
    var orchestrator = host.Services.GetRequiredService<IImageProcessingOrchestrator>();
    var exitCode = await orchestrator.ProcessAllImagesAsync();
    Environment.Exit(exitCode);
}
catch (Exception ex)
{
    var logger = host.Services.GetRequiredService<ILogger<Program>>();
    logger.LogCritical(ex, "Application terminated unexpectedly");
    Environment.Exit(1);
}

public partial class Program { }
