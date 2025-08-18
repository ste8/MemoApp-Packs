# ImagesGenerator

A .NET 9 console application that batch-generates cartoon-style images using the OpenAI Images API (DALL-E 3).

## Prerequisites

- .NET 9 SDK
- OpenAI API key with access to DALL-E 3

## Setup

### Setting the OpenAI API Key

The application uses a configuration system that supports multiple ways to provide your API key:

#### Method 1: Using appsettings.Development.json (Recommended for local development)

1. Open `appsettings.Development.json`
2. Replace `PUT_YOUR_ACTUAL_API_KEY_HERE` with your actual OpenAI API key
3. This file is already excluded from version control via `.gitignore`

#### Method 2: Using Environment Variables

Set the `OPENAI_API_KEY` environment variable:

**Windows (Command Prompt):**
```cmd
set OPENAI_API_KEY=your_api_key_here
```

**Windows (PowerShell):**
```powershell
$env:OPENAI_API_KEY="your_api_key_here"
```

**macOS/Linux:**
```bash
export OPENAI_API_KEY="your_api_key_here"
```

#### Method 3: Using appsettings.json (Not recommended for shared repositories)

Update the `OpenAI:ApiKey` value in `appsettings.json`. Note: Be careful not to commit this change to version control.

### Configuration Priority

The application loads configuration in the following order (later sources override earlier ones):
1. `appsettings.json`
2. `appsettings.Development.json` (if exists)
3. Environment variables

This means environment variables will override settings from JSON files.

## Building and Running

### Build the project:
```bash
cd ImagesGenerator
dotnet build
```

### Run the application:
```bash
dotnet run
```

## Output

Generated images are saved in the `out/` directory (created automatically next to the executable). Each image is named using the format: `{number}_{word}.png`

Example output:
- `out/4_Arrow.png`
- `out/5_Whale.png`
- `out/08_Sofa.png`
- etc.

## Configuration

### Application Settings

The application can be configured through `appsettings.json`:

```json
{
  "OpenAI": {
    "ApiKey": "YOUR_API_KEY_HERE"
  },
  "ImageGeneration": {
    "Model": "dall-e-3",
    "Size": "1024x1024",
    "Quality": "standard",
    "OutputDirectory": "out"
  }
}
```

### Changing the Subjects

To modify the list of subjects for image generation, edit the `Subjects` list in `Program.cs`:

```csharp
private static readonly List<(string Number, string Word)> Subjects = new()
{
    ("4", "Arrow"),
    ("5", "Whale"),
    // Add or modify entries here
};
```

### Changing Image Size

To change the image size, modify the `ImageGenerationOptions` in the `GenerateImageWithRetryAsync` method:

```csharp
var options = new ImageGenerationOptions
{
    Size = GeneratedImageSize.W1024xH1024, // Change to W512xW512 or W1792xH1024
    // ...
};
```

Available sizes for DALL-E 3:
- `GeneratedImageSize.W1024xH1024` (default)
- `GeneratedImageSize.W1024xH1792` 
- `GeneratedImageSize.W1792xH1024`

### Modifying the Prompt

The prompt template is defined in the `PromptTemplate` constant. You can modify it to change the style or characteristics of the generated images.

### Security Note

- **Never commit your actual API key to version control**
- The `.gitignore` file is configured to exclude `appsettings.Development.json` and other sensitive files
- Always use `appsettings.Development.json` or environment variables for API keys in development

## Features

- **Automatic retry logic**: Implements exponential backoff for handling rate limits and transient errors
- **Error handling**: Continues processing remaining items if one fails
- **Progress logging**: Shows status for each image generation
- **Filename sanitization**: Ensures safe filenames for all operating systems
- **Output organization**: All images saved to a dedicated `out/` directory

## Error Handling

The application will:
- Exit with code 1 if the API key is not set
- Continue processing other images if one fails
- Exit with code 1 if any images failed to generate (after retries)
- Log all errors and retry attempts to the console