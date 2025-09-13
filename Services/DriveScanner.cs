using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using MediaScanner.Models;

namespace MediaScanner.Services;

public class DriveScanner : IDriveScanner
{
    private readonly ILogger<DriveScanner> _logger;
    
    // Supported media file extensions
    private static readonly HashSet<string> VideoExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp4", ".avi", ".mkv", ".mov", ".wmv", ".flv", ".webm", ".m4v", ".mpg", ".mpeg", ".3gp"
    };
    
    private static readonly HashSet<string> AudioExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp3", ".flac", ".wav", ".aac", ".ogg", ".wma", ".m4a", ".opus", ".aiff"
    };
    
    private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".tiff", ".webp"
    };
    
    // Regex patterns for detecting TV shows
    private static readonly Regex TvShowPattern = new(@"[Ss](\d{1,2})[Ee](\d{1,2})", RegexOptions.Compiled);
    private static readonly Regex SeasonFolderPattern = new(@"Season\s*(\d{1,2})", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public DriveScanner(ILogger<DriveScanner> logger)
    {
        _logger = logger;
    }

    public async Task<List<NetworkDrive>> ScanForNetworkDrivesAsync()
    {
        var drives = new List<NetworkDrive>();
        
        try
        {
            // Scan for mapped network drives
            var allDrives = DriveInfo.GetDrives();
            foreach (var drive in allDrives)
            {
                if (drive.DriveType == System.IO.DriveType.Network && drive.IsReady)
                {
                    drives.Add(new NetworkDrive
                    {
                        Name = $"Network Drive ({drive.Name.TrimEnd('\\', '/')})",
                        Path = drive.RootDirectory.FullName,
                        Type = Models.DriveType.NetworkShare,
                        IsAvailable = true,
                        Description = $"Network drive at {drive.Name}"
                    });
                }
                else if (drive.DriveType == System.IO.DriveType.Removable && drive.IsReady)
                {
                    drives.Add(new NetworkDrive
                    {
                        Name = $"USB Drive ({drive.Name.TrimEnd('\\', '/')})",
                        Path = drive.RootDirectory.FullName,
                        Type = Models.DriveType.UsbDrive,
                        IsAvailable = true,
                        Description = $"USB drive at {drive.Name}"
                    });
                }
            }
            
            // Try to discover UNC paths and common NAS devices
            await DiscoverNetworkSharesAsync(drives);
            
            _logger.LogInformation($"Found {drives.Count} available drives");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scanning for network drives");
        }
        
        return drives;
    }

    private async Task DiscoverNetworkSharesAsync(List<NetworkDrive> drives)
    {
        // Common NAS device names and patterns
        var commonNasNames = new[]
        {
            "DLINK-SHARECENTER",
            "SYNOLOGY",
            "QNAP",
            "BUFFALO",
            "NETGEAR",
            "WD-MYCLOUD"
        };
        
        // Try to ping and discover common NAS devices on the network
        // This is a simplified approach - in production, you'd use more sophisticated network discovery
        foreach (var nasName in commonNasNames)
        {
            try
            {
                var uncPath = $@"\\{nasName}";
                if (Directory.Exists(uncPath))
                {
                    var shares = Directory.GetDirectories(uncPath);
                    foreach (var share in shares)
                    {
                        var shareName = Path.GetFileName(share);
                        drives.Add(new NetworkDrive
                        {
                            Name = $"{nasName} - {shareName}",
                            Path = share,
                            Type = Models.DriveType.NetworkShare,
                            IsAvailable = true,
                            Description = $"Network share on {nasName}"
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, $"Could not access {nasName}");
            }
        }
        
        await Task.CompletedTask; // Placeholder for async network discovery
    }

    public async Task<List<MediaItem>> ScanDriveForMediaAsync(NetworkDrive drive, IProgress<string>? progress = null, CancellationToken cancellationToken = default)
    {
        var mediaItems = new List<MediaItem>();
        
        try
        {
            progress?.Report($"Scanning {drive.Name}...");
            
            var mediaFiles = await GetMediaFilesAsync(drive.Path, cancellationToken);
            
            var totalFiles = mediaFiles.Count;
            var processedFiles = 0;
            
            foreach (var filePath in mediaFiles)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;
                
                try
                {
                    var mediaItem = await CreateMediaItemFromFileAsync(filePath);
                    if (mediaItem != null)
                    {
                        mediaItems.Add(mediaItem);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, $"Error processing file: {filePath}");
                }
                
                processedFiles++;
                progress?.Report($"Processing {processedFiles}/{totalFiles}: {Path.GetFileName(filePath)}");
            }
            
            _logger.LogInformation($"Scanned {drive.Name}: found {mediaItems.Count} media items");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error scanning drive: {drive.Name}");
        }
        
        return mediaItems;
    }

    public async Task<List<string>> GetMediaFilesAsync(string path, CancellationToken cancellationToken = default)
    {
        var mediaFiles = new List<string>();
        
        try
        {
            await Task.Run(() =>
            {
                var files = Directory.GetFiles(path, "*", SearchOption.AllDirectories);
                
                foreach (var file in files)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;
                    
                    if (IsMediaFile(file))
                    {
                        mediaFiles.Add(file);
                    }
                }
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting media files from: {path}");
        }
        
        return mediaFiles;
    }

    public bool IsMediaFile(string filePath)
    {
        var extension = Path.GetExtension(filePath);
        return VideoExtensions.Contains(extension) || AudioExtensions.Contains(extension);
    }

    public MediaType DetectMediaType(string filePath)
    {
        var extension = Path.GetExtension(filePath);
        var fileName = Path.GetFileNameWithoutExtension(filePath);
        var directory = Path.GetDirectoryName(filePath) ?? string.Empty;
        
        if (AudioExtensions.Contains(extension))
        {
            return MediaType.Music;
        }
        
        if (!VideoExtensions.Contains(extension))
        {
            return MediaType.Other;
        }
        
        // Check if it's a TV show based on filename pattern or directory structure
        if (TvShowPattern.IsMatch(fileName) || SeasonFolderPattern.IsMatch(directory))
        {
            return MediaType.TVShow;
        }
        
        // Check for common TV show directory patterns
        var pathParts = directory.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        foreach (var part in pathParts)
        {
            if (SeasonFolderPattern.IsMatch(part) || 
                part.Contains("TV", StringComparison.OrdinalIgnoreCase) ||
                part.Contains("Series", StringComparison.OrdinalIgnoreCase) ||
                part.Contains("Episodes", StringComparison.OrdinalIgnoreCase))
            {
                return MediaType.TVShow;
            }
        }
        
        // Default to movie for video files
        return MediaType.Movie;
    }

    private async Task<MediaItem?> CreateMediaItemFromFileAsync(string filePath)
    {
        try
        {
            var fileInfo = new FileInfo(filePath);
            var fileName = Path.GetFileNameWithoutExtension(filePath);
            var mediaType = DetectMediaType(filePath);
            
            var mediaItem = new MediaItem
            {
                Title = CleanupTitle(fileName),
                OriginalFileName = fileInfo.Name,
                FilePath = filePath,
                FileSize = fileInfo.Length,
                DateModified = fileInfo.LastWriteTime,
                Type = mediaType
            };
            
            // Extract additional information based on type
            if (mediaType == MediaType.TVShow)
            {
                ExtractTVShowInfo(mediaItem, fileName, filePath);
            }
            else if (mediaType == MediaType.Music)
            {
                await ExtractMusicInfoAsync(mediaItem, filePath);
            }
            else if (mediaType == MediaType.Movie)
            {
                ExtractMovieInfo(mediaItem, fileName);
            }
            
            return mediaItem;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, $"Error creating media item from file: {filePath}");
            return null;
        }
    }

    private string CleanupTitle(string title)
    {
        // Remove common patterns and clean up the title
        title = Regex.Replace(title, @"\[.*?\]", "").Trim(); // Remove [bracketed] content
        title = Regex.Replace(title, @"\(.*?\)", "").Trim(); // Remove (parenthetical) content
        title = Regex.Replace(title, @"\d{4}", "").Trim(); // Remove year
        title = Regex.Replace(title, @"[Ss]\d{1,2}[Ee]\d{1,2}", "").Trim(); // Remove S01E01 pattern
        title = Regex.Replace(title, @"[-_.]+", " ").Trim(); // Replace separators with spaces
        title = Regex.Replace(title, @"\s+", " ").Trim(); // Normalize whitespace
        
        return title;
    }

    private void ExtractTVShowInfo(MediaItem mediaItem, string fileName, string filePath)
    {
        var match = TvShowPattern.Match(fileName);
        if (match.Success)
        {
            if (int.TryParse(match.Groups[1].Value, out var season))
                mediaItem.Season = season;
            
            if (int.TryParse(match.Groups[2].Value, out var episode))
                mediaItem.Episode = episode;
        }
        
        // Try to extract series name from directory structure
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory))
        {
            var pathParts = directory.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            // Look for the parent directory that's not "Season X"
            for (int i = pathParts.Length - 1; i >= 0; i--)
            {
                if (!SeasonFolderPattern.IsMatch(pathParts[i]) && !string.IsNullOrEmpty(pathParts[i]))
                {
                    mediaItem.SeriesName = pathParts[i];
                    break;
                }
            }
        }
    }

    private async Task ExtractMusicInfoAsync(MediaItem mediaItem, string filePath)
    {
        // This is a placeholder - in a real implementation, you'd use a library like TagLib# to read metadata
        await Task.CompletedTask;
        
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory))
        {
            var pathParts = directory.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (pathParts.Length >= 2)
            {
                mediaItem.Artist = pathParts[^2]; // Second to last directory (Artist)
                mediaItem.Album = pathParts[^1];  // Last directory (Album)
            }
        }
    }

    private void ExtractMovieInfo(MediaItem mediaItem, string fileName)
    {
        // Try to extract year from filename
        var yearMatch = Regex.Match(fileName, @"\((\d{4})\)");
        if (!yearMatch.Success)
        {
            yearMatch = Regex.Match(fileName, @"(\d{4})");
        }
        
        if (yearMatch.Success && int.TryParse(yearMatch.Groups[1].Value, out var year))
        {
            if (year >= 1900 && year <= DateTime.Now.Year + 5) // Reasonable year range
            {
                mediaItem.Year = year;
            }
        }
    }
}