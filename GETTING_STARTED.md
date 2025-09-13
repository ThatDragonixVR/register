# Getting Started with Media Scanner

## Quick Start

1. **Install Prerequisites**
   - Download and install [.NET 8.0 Runtime](https://dotnet.microsoft.com/download/dotnet/8.0)
   - Ensure you have Windows 10/11

2. **Get API Key**
   - Visit [OMDB API](http://www.omdbapi.com/) 
   - Sign up for a free API key
   - Open `Services/MetadataService.cs`
   - Replace `YOUR_OMDB_API_KEY_HERE` with your actual API key

3. **Launch Application**
   - **Option A**: Double-click `run.bat`
   - **Option B**: Right-click `run.ps1` → "Run with PowerShell"
   - **Option C**: Open terminal and run `dotnet run`

## What You Get

### 🖥️ Modern Windows Application
- Clean, Material Design UI
- Responsive and user-friendly interface
- Real-time search and filtering

### 🔍 Network Drive Discovery
- Automatically finds D-Link ShareCenter and other NAS devices
- Discovers USB drives and network shares
- Supports common NAS brands (Synology, QNAP, Buffalo, etc.)

### 📁 Media File Detection
- **Movies**: MP4, AVI, MKV, MOV, WMV, and more
- **TV Shows**: Automatic episode detection (S01E01 format)
- **Music**: MP3, FLAC, WAV, AAC, and more
- Smart categorization based on file names and folder structure

### 🌐 Metadata Fetching
- **Movies & TV**: IMDB ratings, posters, cast, plot summaries
- **Music**: Artist, album, release year information
- Automatic background processing
- Local caching for fast access

### 🗃️ Local Database
- SQLite database for fast searches
- Caches all metadata locally
- Persistent across application restarts

## First Use Workflow

1. **Launch** the application
2. **Scan for Drives** using the refresh button
3. **Select a drive** from the left panel
4. **Click scan** to start finding media files
5. **Browse your library** as metadata is fetched automatically

## Project Structure

```
MediaScanner/
├── Models/                 # Data models
│   ├── MediaItem.cs       # Main media file model
│   ├── NetworkDrive.cs    # Network drive model
│   └── MetadataResponse.cs # API response models
├── Services/              # Business logic
│   ├── DriveScanner.cs    # File system scanning
│   ├── MetadataService.cs # Online metadata fetching
│   ├── DatabaseService.cs # SQLite operations
│   └── MediaLibraryService.cs # Main orchestration
├── ViewModels/            # MVVM view models
│   └── MainViewModel.cs   # Main window logic
├── Views/                 # Additional UI components
│   └── MediaDetailsDialog.xaml # Media details view
├── Converters/            # UI value converters
│   └── NullToVisibilityConverter.cs
├── MainWindow.xaml        # Main application window
├── App.xaml              # Application resources
└── README.md             # Full documentation
```

## Features Explained

### Drive Scanning
- Scans Windows drive letters for network and USB drives
- Attempts to discover common NAS hostnames on your network
- Shows drive status and last scan information

### Media Detection
- Uses file extensions and naming patterns
- Detects TV shows by S##E## patterns and season folders
- Organizes music by Artist/Album folder structure
- Handles movie files in dedicated directories

### Metadata Sources
- **OMDB API**: Movies and TV shows (requires free API key)
- **MusicBrainz**: Music information (no key required)
- Rate-limited to respect API terms of service

### Search & Filtering
- Real-time search across titles, artists, directors, genres
- Filter by media type (Movies, TV Shows, Music, Other)
- Results update instantly as you type

## Troubleshooting

### "API key not found" errors
- Make sure you've updated `Services/MetadataService.cs` with your OMDB API key
- Restart the application after making changes

### Network drives not showing
- Ensure drives are accessible from Windows File Explorer first
- Check network connectivity
- Try mapping drives manually in Windows

### Slow scanning
- Large libraries take time to scan initially
- Metadata fetching continues in background
- Subsequent scans are much faster due to caching

### Build errors
- Ensure .NET 8.0 SDK is installed (not just runtime)
- Run `dotnet restore` to download dependencies
- Check that all files are present and not corrupted

## Need Help?

1. Check the full `README.md` for detailed information
2. Verify all prerequisites are installed
3. Ensure API keys are configured correctly
4. Check Windows Event Viewer for detailed error logs

Enjoy organizing your media library! 🎬🎵📺