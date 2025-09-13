using MediaScanner.Models;

namespace MediaScanner.Services;

public interface IMediaLibraryService
{
    Task<List<MediaItem>> GetAllMediaAsync();
    Task<List<MediaItem>> SearchMediaAsync(string searchTerm);
    Task<List<MediaItem>> GetMediaByTypeAsync(MediaType mediaType);
    Task ScanAndUpdateLibraryAsync(NetworkDrive drive, IProgress<string>? progress = null, CancellationToken cancellationToken = default);
    Task RefreshMetadataAsync(MediaItem mediaItem, CancellationToken cancellationToken = default);
    Task DeleteMediaItemAsync(int id);
    Task InitializeAsync();
}