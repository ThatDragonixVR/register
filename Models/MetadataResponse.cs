using Newtonsoft.Json;

namespace MediaScanner.Models;

// OMDB API Response Models
public class OmdbResponse
{
    [JsonProperty("Title")]
    public string? Title { get; set; }
    
    [JsonProperty("Year")]
    public string? Year { get; set; }
    
    [JsonProperty("Rated")]
    public string? Rated { get; set; }
    
    [JsonProperty("Released")]
    public string? Released { get; set; }
    
    [JsonProperty("Runtime")]
    public string? Runtime { get; set; }
    
    [JsonProperty("Genre")]
    public string? Genre { get; set; }
    
    [JsonProperty("Director")]
    public string? Director { get; set; }
    
    [JsonProperty("Actors")]
    public string? Actors { get; set; }
    
    [JsonProperty("Plot")]
    public string? Plot { get; set; }
    
    [JsonProperty("Poster")]
    public string? Poster { get; set; }
    
    [JsonProperty("imdbRating")]
    public string? ImdbRating { get; set; }
    
    [JsonProperty("imdbID")]
    public string? ImdbID { get; set; }
    
    [JsonProperty("Type")]
    public string? Type { get; set; }
    
    [JsonProperty("Response")]
    public string? Response { get; set; }
    
    [JsonProperty("Error")]
    public string? Error { get; set; }
    
    // TV Show specific
    [JsonProperty("Season")]
    public string? Season { get; set; }
    
    [JsonProperty("Episode")]
    public string? Episode { get; set; }
    
    [JsonProperty("seriesID")]
    public string? SeriesID { get; set; }
}

// MusicBrainz API Response Models
public class MusicBrainzResponse
{
    [JsonProperty("recordings")]
    public MusicRecording[]? Recordings { get; set; }
}

public class MusicRecording
{
    [JsonProperty("title")]
    public string? Title { get; set; }
    
    [JsonProperty("length")]
    public int? Length { get; set; }
    
    [JsonProperty("artist-credit")]
    public ArtistCredit[]? ArtistCredit { get; set; }
    
    [JsonProperty("releases")]
    public MusicRelease[]? Releases { get; set; }
}

public class ArtistCredit
{
    [JsonProperty("name")]
    public string? Name { get; set; }
    
    [JsonProperty("artist")]
    public Artist? Artist { get; set; }
}

public class Artist
{
    [JsonProperty("name")]
    public string? Name { get; set; }
}

public class MusicRelease
{
    [JsonProperty("title")]
    public string? Title { get; set; }
    
    [JsonProperty("date")]
    public string? Date { get; set; }
}