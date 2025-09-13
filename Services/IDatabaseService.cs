using MediaScanner.Models;

namespace MediaScanner.Services;

public interface IDatabaseService
{
    Task InitializeAsync();
    Task<List<MediaItem>> GetAllMediaItemsAsync();
    Task<MediaItem?> GetMediaItemByPathAsync(string filePath);
    Task<int> SaveMediaItemAsync(MediaItem mediaItem);
    Task UpdateMediaItemAsync(MediaItem mediaItem);
    Task DeleteMediaItemAsync(int id);
    Task DeleteMediaItemsByDriveAsync(string drivePath);
    Task<List<MediaItem>> SearchMediaItemsAsync(string searchTerm);
    Task<List<MediaItem>> GetMediaItemsByTypeAsync(MediaType mediaType);
}