using Microsoft.Extensions.Logging;
using MediaScanner.Models;

namespace MediaScanner.Services;

public class MediaLibraryService : IMediaLibraryService
{
    private readonly IDatabaseService _databaseService;
    private readonly IDriveScanner _driveScanner;
    private readonly IMetadataService _metadataService;
    private readonly ILogger<MediaLibraryService> _logger;

    public MediaLibraryService(
        IDatabaseService databaseService,
        IDriveScanner driveScanner,
        IMetadataService metadataService,
        ILogger<MediaLibraryService> logger)
    {
        _databaseService = databaseService;
        _driveScanner = driveScanner;
        _metadataService = metadataService;
        _logger = logger;
    }

    public async Task InitializeAsync()
    {
        await _databaseService.InitializeAsync();
    }

    public async Task<List<MediaItem>> GetAllMediaAsync()
    {
        return await _databaseService.GetAllMediaItemsAsync();
    }

    public async Task<List<MediaItem>> SearchMediaAsync(string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return await GetAllMediaAsync();
        }

        return await _databaseService.SearchMediaItemsAsync(searchTerm);
    }

    public async Task<List<MediaItem>> GetMediaByTypeAsync(MediaType mediaType)
    {
        return await _databaseService.GetMediaItemsByTypeAsync(mediaType);
    }

    public async Task ScanAndUpdateLibraryAsync(NetworkDrive drive, IProgress<string>? progress = null, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation($"Starting scan of drive: {drive.Name}");
            
            progress?.Report($"Scanning {drive.Name} for media files...");
            
            // Scan drive for media files
            var scannedItems = await _driveScanner.ScanDriveForMediaAsync(drive, progress, cancellationToken);
            
            progress?.Report("Processing and saving media items...");
            
            var totalItems = scannedItems.Count;
            var processedItems = 0;
            
            foreach (var mediaItem in scannedItems)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;
                
                try
                {
                    // Check if item already exists in database
                    var existingItem = await _databaseService.GetMediaItemByPathAsync(mediaItem.FilePath);
                    
                    if (existingItem == null)
                    {
                        // New item - save to database first, then fetch metadata
                        var itemId = await _databaseService.SaveMediaItemAsync(mediaItem);
                        mediaItem.Id = itemId;
                        
                        // Fetch metadata in background (don't wait for it to complete)
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                await _metadataService.FetchMetadataAsync(mediaItem, cancellationToken);
                                await _databaseService.UpdateMediaItemAsync(mediaItem);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, $"Failed to fetch metadata for {mediaItem.Title}");
                            }
                        }, cancellationToken);
                    }
                    else
                    {
                        // Update existing item if file has been modified
                        if (mediaItem.DateModified > existingItem.DateModified)
                        {
                            existingItem.FileSize = mediaItem.FileSize;
                            existingItem.DateModified = mediaItem.DateModified;
                            await _databaseService.UpdateMediaItemAsync(existingItem);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, $"Error processing media item: {mediaItem.FilePath}");
                }
                
                processedItems++;
                progress?.Report($"Processed {processedItems}/{totalItems} items");
            }
            
            progress?.Report($"Scan complete. Found {scannedItems.Count} media items.");
            _logger.LogInformation($"Completed scan of drive: {drive.Name}. Found {scannedItems.Count} items.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error scanning drive: {drive.Name}");
            progress?.Report($"Error scanning {drive.Name}: {ex.Message}");
            throw;
        }
    }

    public async Task RefreshMetadataAsync(MediaItem mediaItem, CancellationToken cancellationToken = default)
    {
        try
        {
            var success = await _metadataService.FetchMetadataAsync(mediaItem, cancellationToken);
            if (success)
            {
                await _databaseService.UpdateMediaItemAsync(mediaItem);
                _logger.LogInformation($"Refreshed metadata for: {mediaItem.Title}");
            }
            else
            {
                _logger.LogWarning($"Failed to refresh metadata for: {mediaItem.Title}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error refreshing metadata for: {mediaItem.Title}");
            throw;
        }
    }

    public async Task DeleteMediaItemAsync(int id)
    {
        try
        {
            await _databaseService.DeleteMediaItemAsync(id);
            _logger.LogInformation($"Deleted media item with ID: {id}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error deleting media item with ID: {id}");
            throw;
        }
    }
}