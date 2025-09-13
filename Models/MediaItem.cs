using System;
using System.ComponentModel.DataAnnotations;

namespace MediaScanner.Models;

public class MediaItem
{
    public int Id { get; set; }
    
    [Required]
    public string Title { get; set; } = string.Empty;
    
    public string? OriginalFileName { get; set; }
    
    [Required]
    public string FilePath { get; set; } = string.Empty;
    
    public long FileSize { get; set; }
    
    public DateTime DateAdded { get; set; } = DateTime.Now;
    
    public DateTime? DateModified { get; set; }
    
    [Required]
    public MediaType Type { get; set; }
    
    // Metadata from external sources
    public string? ImdbId { get; set; }
    public string? TmdbId { get; set; }
    public string? Description { get; set; }
    public int? Year { get; set; }
    public string? Genre { get; set; }
    public double? Rating { get; set; }
    public string? Director { get; set; }
    public string? Cast { get; set; }
    public string? PosterUrl { get; set; }
    public string? BackdropUrl { get; set; }
    public int? Runtime { get; set; }
    
    // TV Show specific
    public int? Season { get; set; }
    public int? Episode { get; set; }
    public string? SeriesName { get; set; }
    
    // Music specific
    public string? Artist { get; set; }
    public string? Album { get; set; }
    public int? Track { get; set; }
    public int? Duration { get; set; }
    
    // Computed properties for UI
    public string TypeIcon => Type switch
    {
        MediaType.Movie => "Movie",
        MediaType.TVShow => "Television",
        MediaType.Music => "Music",
        MediaType.Other => "File",
        _ => "File"
    };
    
    public string DisplayTitle => Type switch
    {
        MediaType.TVShow when !string.IsNullOrEmpty(SeriesName) => 
            $"{SeriesName} - S{Season:D2}E{Episode:D2}",
        MediaType.Music when !string.IsNullOrEmpty(Artist) => 
            $"{Artist} - {Title}",
        _ => Title
    };
}

public enum MediaType
{
    Movie,
    TVShow,
    Music,
    Other
}