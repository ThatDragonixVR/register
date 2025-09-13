using MediaScanner.Models;

namespace MediaScanner.Services;

public interface IMetadataService
{
    Task<bool> FetchMetadataAsync(MediaItem mediaItem, CancellationToken cancellationToken = default);
    Task<OmdbResponse?> SearchMovieAsync(string title, int? year = null, CancellationToken cancellationToken = default);
    Task<OmdbResponse?> SearchTVShowAsync(string seriesName, int? season = null, int? episode = null, CancellationToken cancellationToken = default);
    Task<MusicBrainzResponse?> SearchMusicAsync(string artist, string title, CancellationToken cancellationToken = default);
}