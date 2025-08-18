# Packs Builder

A .NET 9 console application for managing major system packs for a memorization training app. The application handles creating zip files, initializing default configurations, and maintaining a master index of available packs for download.

## Features

- **Zip Creation**: Automatically create zip files from major system pack folders
- **Defaults Management**: Initialize and intelligently merge defaults.json files
- **Master Index**: Maintain a centralized index of all available packs with download URLs
- **Configuration**: Flexible configuration through appsettings.json and command-line arguments
- **Cross-platform**: Works on Windows, macOS, and Linux

## Prerequisites

- .NET 9 SDK or later

## Installation

1. Clone the repository
2. Navigate to the PacksBuilder directory
3. Run `dotnet build`

## Usage

### Running the Application

```bash
dotnet run
```

Or with command-line overrides:

```bash
dotnet run -- --base-url https://cdn.example.com/packs/ --output-dir ./custom-output
```

### Menu Options

1. **Create zip files for all major system packs and update master index**
   - Scans a root folder for pack directories
   - Creates zip files for each pack (if not already existing)
   - Updates the master index with pack information

2. **Initialize/update defaults.json for a specific pack**
   - Scans a pack's images directory
   - Creates or merges defaults.json with intelligent conflict resolution
   - Preserves existing customizations while adding new defaults

3. **Regenerate master index file**
   - Scans existing zip files in the output directory
   - Rebuilds the master index from scratch

4. **Exit**

## Major System Pack Structure

Each pack folder should follow this structure:

```
pack_folder/
├── manifest.json       # Pack metadata
├── defaults.json       # Default image selections (auto-generated)
├── images/            # Image files organized by category
│   ├── numbers/
│   ├── letters_upper/
│   ├── letters_lower/
│   ├── symbols/
│   └── ...
└── thumbnails/        # Thumbnail versions (optional)
    └── ...
```

### manifest.json Format

```json
{
  "international_name": "Italian",
  "native_name": "Italiano",
  "description": "Traditional Italian major system",
  "native_description": "Sistema maggiore tradizionale italiano",
  "version": "1.0.0",
  "language_code": "it",
  "author": "CompanyX"
}
```

### Image Naming Convention

Images should follow the format: `{token}_{word}.{extension}`

Examples:
- `00_sasso.png`
- `A_apple.jpg`
- `@_at.png`

## Configuration

### appsettings.json

```json
{
  "PackSettings": {
    "BaseDownloadUrl": "https://yourserver.com/packs/",
    "OutputDirectory": "./output"
  }
}
```

### Command-Line Arguments

- `--base-url <url>`: Override the base download URL
- `--output-dir <path>`: Override the output directory

## Output Files

### Zip Files

- Named as: `{international_name}_{version}.zip`
- Example: `italian_1.0.0.zip`
- Contains the complete pack folder structure

### major_system_packs.json

Master index file containing:
- Pack metadata from manifest.json
- File size information
- Download URLs (constructed from base URL)
- Last updated timestamp

## Testing

Run the test suite:

```bash
dotnet test
```

## Sample Test Data

The TestData folder contains sample pack structures for testing:
- Italian pack with numbers, letters, and symbols
- Spanish pack with basic structure

## Development

### Project Structure

```
PacksBuilder/
├── Models/              # Data models
├── Services/            # Business logic
├── Configuration/       # Configuration classes
├── TestData/           # Sample pack data
└── Program.cs          # Main application entry point

PacksBuilder.Tests/     # Unit tests
```

### Key Services

- **FileDiscoveryService**: Discovers packs and images
- **DefaultsService**: Manages defaults.json files
- **ZipService**: Creates zip archives
- **MasterIndexService**: Manages the master index

## License

[Your License Here]