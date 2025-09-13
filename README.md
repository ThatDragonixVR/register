# Media Scanner

A Windows WPF application that scans network drives (like D-Link ShareCenter) and external drives for media files, automatically fetching metadata from online sources like IMDB to create a comprehensive media library.

## Features

- **Network Drive Discovery**: Automatically discovers and connects to network shares and USB drives
- **Media File Detection**: Supports movies, TV shows, music, and other media formats
- **Metadata Fetching**: Retrieves information from OMDB API (movies/TV) and MusicBrainz (music)
- **Modern UI**: Clean, Material Design-inspired interface
- **Search & Filtering**: Advanced search and filtering capabilities
- **Local Database**: SQLite database for caching metadata and fast access
- **Background Processing**: Non-blocking scanning and metadata fetching

## Supported File Formats

### Video Files
- MP4, AVI, MKV, MOV, WMV, FLV, WebM, M4V, MPG, MPEG, 3GP

### Audio Files
- MP3, FLAC, WAV, AAC, OGG, WMA, M4A, Opus, AIFF

### Image Files
- JPG, JPEG, PNG, GIF, BMP, TIFF, WebP

## Setup Instructions

### Prerequisites
- Windows 10/11
- .NET 8.0 Runtime
- Internet connection for metadata fetching

### Installation

1. **Clone or download the project**
2. **Get API Keys**:
   - Visit [OMDB API](http://www.omdbapi.com/) and get a free API key
   - Open `Services/MetadataService.cs` and replace `YOUR_OMDB_API_KEY_HERE` with your actual API key

3. **Build and run**:
   ```bash
   dotnet restore
   dotnet build
   dotnet run
   ```

### First Run

1. **Scan for Drives**: Click the refresh button to discover available network drives and USB drives
2. **Connect to Network Shares**: The app will automatically try to discover common NAS devices like:
   - D-Link ShareCenter
   - Synology NAS
   - QNAP NAS
   - Buffalo NAS
   - Netgear NAS
   - WD MyCloud

3. **Scan Media**: Click the scan button next to any discovered drive to start scanning for media files
4. **View Results**: Media items will appear in the main library view with fetched metadata

## Configuration

### Network Discovery
The app automatically scans for:
- Mapped network drives
- USB/removable drives  
- Common NAS device hostnames on the local network

### Metadata Sources
- **Movies & TV Shows**: OMDB API (requires free API key)
- **Music**: MusicBrainz API (no key required)

### Database Location
The SQLite database is stored at:
`%APPDATA%\MediaScanner\media.db`

## Usage

### Scanning Drives
1. Use the "Scan for Network Drives" button to discover available drives
2. Select a drive from the left panel
3. Click the scan button to start scanning for media files
4. The app will automatically detect file types and fetch metadata

### Searching and Filtering
- Use the search box to find specific titles, artists, or series
- Toggle media type filters (Movies, TV Shows, Music, Other)
- Results update in real-time as you type

### Media Types
The app automatically categorizes files as:
- **Movies**: Video files in movie directories or with movie-like names
- **TV Shows**: Files matching TV episode patterns (S01E01) or in season folders
- **Music**: Audio files, preferably organized in Artist/Album folder structure
- **Other**: Any other supported media files

## Architecture

### Services
- **DriveScanner**: Discovers network drives and scans for media files
- **MetadataService**: Fetches metadata from external APIs
- **DatabaseService**: Manages SQLite database operations
- **MediaLibraryService**: Orchestrates scanning and library management

### UI Framework
- **WPF** with **Material Design** themes
- **MVVM** pattern using CommunityToolkit.Mvvm
- **Dependency Injection** with Microsoft.Extensions.DependencyInjection

## Troubleshooting

### Network Drive Access Issues
- Ensure the network drive is accessible from your computer
- Check Windows network credentials
- Try mapping the drive manually first

### API Key Issues
- Verify your OMDB API key is correct
- Check your internet connection
- API has rate limits - scanning large libraries may take time

### Performance
- Large libraries (1000+ files) may take time to scan initially
- Metadata fetching happens in background and may continue after scan completes
- Database caches all metadata for fast subsequent access

## Development

### Adding New Metadata Sources
1. Implement new response models in `Models/MetadataResponse.cs`
2. Add new methods to `IMetadataService` and `MetadataService`
3. Update `MediaLibraryService` to use new metadata sources

### Adding New File Types
1. Update file extension lists in `DriveScanner.cs`
2. Add new `MediaType` enum values if needed
3. Update UI to handle new media types

## License

This project is provided as-is for educational and personal use.

## API Credits

- Movie/TV metadata provided by [OMDB API](http://www.omdbapi.com/)
- Music metadata provided by [MusicBrainz](https://musicbrainz.org/)