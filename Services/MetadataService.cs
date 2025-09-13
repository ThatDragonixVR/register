using System.Net.Http;
using System.Web;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using MediaScanner.Models;

namespace MediaScanner.Services;

public class MetadataService : IMetadataService, IDisposable
{
    private readonly ILogger<MetadataService> _logger;
    private readonly HttpClient _httpClient;
    
    // OMDB API - You'll need to get a free API key from http://www.omdbapi.com/
    private const string OmdbApiKey = "YOUR_OMDB_API_KEY_HERE"; // Replace with your actual API key
    private const string OmdbBaseUrl = "http://www.omdbapi.com/";
    
    // MusicBrainz API (no key required)
    private const string MusicBrainzBaseUrl = "https://musicbrainz.org/ws/2/";
    
    public MetadataService(ILogger<MetadataService> logger)
    {
        _logger = logger;
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "MediaScanner/1.0 (https://github.com/yourname/mediascanner)");
    }

    public async Task<bool> FetchMetadataAsync(MediaItem mediaItem, CancellationToken cancellationToken = default)
    {
        try
        {
            switch (mediaItem.Type)
            {
                case MediaType.Movie:
                    return await FetchMovieMetadataAsync(mediaItem, cancellationToken);
                    
                case MediaType.TVShow:
                    return await FetchTVShowMetadataAsync(mediaItem, cancellationToken);
                    
                case MediaType.Music:
                    return await FetchMusicMetadataAsync(mediaItem, cancellationToken);
                    
                default:
                    return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error fetching metadata for {mediaItem.Title}");
            return false;
        }
    }

    public async Task<OmdbResponse?> SearchMovieAsync(string title, int? year = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var queryParams = new Dictionary<string, string>
            {
                ["apikey"] = OmdbApiKey,
                ["t"] = title,
                ["type"] = "movie"
            };
            
            if (year.HasValue)
            {
                queryParams["y"] = year.Value.ToString();
            }
            
            var url = BuildUrl(OmdbBaseUrl, queryParams);
            var response = await _httpClient.GetStringAsync(url, cancellationToken);
            
            var omdbResponse = JsonConvert.DeserializeObject<OmdbResponse>(response);
            
            if (omdbResponse?.Response == "True")
            {
                return omdbResponse;
            }
            
            _logger.LogDebug($"OMDB search failed for movie '{title}': {omdbResponse?.Error}");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error searching for movie: {title}");
            return null;
        }
    }

    public async Task<OmdbResponse?> SearchTVShowAsync(string seriesName, int? season = null, int? episode = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var queryParams = new Dictionary<string, string>
            {
                ["apikey"] = OmdbApiKey,
                ["t"] = seriesName,
                ["type"] = "series"
            };
            
            if (season.HasValue)
            {
                queryParams["Season"] = season.Value.ToString();
            }
            
            if (episode.HasValue)
            {
                queryParams["Episode"] = episode.Value.ToString();
            }
            
            var url = BuildUrl(OmdbBaseUrl, queryParams);
            var response = await _httpClient.GetStringAsync(url, cancellationToken);
            
            var omdbResponse = JsonConvert.DeserializeObject<OmdbResponse>(response);
            
            if (omdbResponse?.Response == "True")
            {
                return omdbResponse;
            }
            
            _logger.LogDebug($"OMDB search failed for TV show '{seriesName}': {omdbResponse?.Error}");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error searching for TV show: {seriesName}");
            return null;
        }
    }

    public async Task<MusicBrainzResponse?> SearchMusicAsync(string artist, string title, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = $"artist:\"{artist}\" AND recording:\"{title}\"";
            var queryParams = new Dictionary<string, string>
            {
                ["query"] = query,
                ["fmt"] = "json",
                ["limit"] = "5"
            };
            
            var url = BuildUrl($"{MusicBrainzBaseUrl}recording", queryParams);
            
            // Add delay to respect MusicBrainz rate limiting
            await Task.Delay(1000, cancellationToken);
            
            var response = await _httpClient.GetStringAsync(url, cancellationToken);
            var musicBrainzResponse = JsonConvert.DeserializeObject<MusicBrainzResponse>(response);
            
            return musicBrainzResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error searching for music: {artist} - {title}");
            return null;
        }
    }

    private async Task<bool> FetchMovieMetadataAsync(MediaItem mediaItem, CancellationToken cancellationToken)
    {
        var omdbResponse = await SearchMovieAsync(mediaItem.Title, mediaItem.Year, cancellationToken);
        
        if (omdbResponse != null)
        {
            ApplyOmdbMetadata(mediaItem, omdbResponse);
            return true;
        }
        
        return false;
    }

    private async Task<bool> FetchTVShowMetadataAsync(MediaItem mediaItem, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(mediaItem.SeriesName))
        {
            return false;
        }
        
        var omdbResponse = await SearchTVShowAsync(mediaItem.SeriesName, mediaItem.Season, mediaItem.Episode, cancellationToken);
        
        if (omdbResponse != null)
        {
            ApplyOmdbMetadata(mediaItem, omdbResponse);
            return true;
        }
        
        return false;
    }

    private async Task<bool> FetchMusicMetadataAsync(MediaItem mediaItem, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(mediaItem.Artist))
        {
            return false;
        }
        
        var musicBrainzResponse = await SearchMusicAsync(mediaItem.Artist, mediaItem.Title, cancellationToken);
        
        if (musicBrainzResponse?.Recordings?.Length > 0)
        {
            ApplyMusicBrainzMetadata(mediaItem, musicBrainzResponse.Recordings[0]);
            return true;
        }
        
        return false;
    }

    private void ApplyOmdbMetadata(MediaItem mediaItem, OmdbResponse omdbResponse)
    {
        mediaItem.Title = omdbResponse.Title ?? mediaItem.Title;
        mediaItem.Description = omdbResponse.Plot;
        mediaItem.Genre = omdbResponse.Genre;
        mediaItem.Director = omdbResponse.Director;
        mediaItem.Cast = omdbResponse.Actors;
        mediaItem.PosterUrl = omdbResponse.Poster != "N/A" ? omdbResponse.Poster : null;
        mediaItem.ImdbId = omdbResponse.ImdbID;
        
        if (int.TryParse(omdbResponse.Year, out var year))
        {
            mediaItem.Year = year;
        }
        
        if (double.TryParse(omdbResponse.ImdbRating, out var rating))
        {
            mediaItem.Rating = rating;
        }
        
        // Parse runtime
        if (!string.IsNullOrEmpty(omdbResponse.Runtime))
        {
            var runtimeMatch = System.Text.RegularExpressions.Regex.Match(omdbResponse.Runtime, @"(\d+)");
            if (runtimeMatch.Success && int.TryParse(runtimeMatch.Groups[1].Value, out var runtime))
            {
                mediaItem.Runtime = runtime;
            }
        }
    }

    private void ApplyMusicBrainzMetadata(MediaItem mediaItem, MusicRecording recording)
    {
        mediaItem.Title = recording.Title ?? mediaItem.Title;
        
        if (recording.ArtistCredit?.Length > 0)
        {
            mediaItem.Artist = recording.ArtistCredit[0].Artist?.Name ?? recording.ArtistCredit[0].Name;
        }
        
        if (recording.Length.HasValue)
        {
            mediaItem.Duration = recording.Length.Value / 1000; // Convert from ms to seconds
        }
        
        if (recording.Releases?.Length > 0)
        {
            mediaItem.Album = recording.Releases[0].Title;
            
            if (DateTime.TryParse(recording.Releases[0].Date, out var releaseDate))
            {
                mediaItem.Year = releaseDate.Year;
            }
        }
    }

    private static string BuildUrl(string baseUrl, Dictionary<string, string> queryParams)
    {
        var query = string.Join("&", queryParams.Select(kvp => $"{HttpUtility.UrlEncode(kvp.Key)}={HttpUtility.UrlEncode(kvp.Value)}"));
        return $"{baseUrl}?{query}";
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}