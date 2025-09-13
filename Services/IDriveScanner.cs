using MediaScanner.Models;

namespace MediaScanner.Services;

public interface IDriveScanner
{
    Task<List<NetworkDrive>> ScanForNetworkDrivesAsync();
    Task<List<MediaItem>> ScanDriveForMediaAsync(NetworkDrive drive, IProgress<string>? progress = null, CancellationToken cancellationToken = default);
    Task<List<string>> GetMediaFilesAsync(string path, CancellationToken cancellationToken = default);
    bool IsMediaFile(string filePath);
    MediaType DetectMediaType(string filePath);
}