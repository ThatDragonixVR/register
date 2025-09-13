using System.Data.SQLite;
using System.Data;
using Microsoft.Extensions.Logging;
using MediaScanner.Models;

namespace MediaScanner.Services;

public class DatabaseService : IDatabaseService
{
    private readonly ILogger<DatabaseService> _logger;
    private readonly string _connectionString;
    
    public DatabaseService(ILogger<DatabaseService> logger)
    {
        _logger = logger;
        var dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MediaScanner", "media.db");
        var dbDirectory = Path.GetDirectoryName(dbPath);
        
        if (!Directory.Exists(dbDirectory))
        {
            Directory.CreateDirectory(dbDirectory!);
        }
        
        _connectionString = $"Data Source={dbPath};Version=3;";
    }

    public async Task InitializeAsync()
    {
        try
        {
            using var connection = new SQLiteConnection(_connectionString);
            await connection.OpenAsync();
            
            var createTableSql = @"
                CREATE TABLE IF NOT EXISTS MediaItems (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Title TEXT NOT NULL,
                    OriginalFileName TEXT,
                    FilePath TEXT NOT NULL UNIQUE,
                    FileSize INTEGER,
                    DateAdded DATETIME DEFAULT CURRENT_TIMESTAMP,
                    DateModified DATETIME,
                    Type INTEGER NOT NULL,
                    ImdbId TEXT,
                    TmdbId TEXT,
                    Description TEXT,
                    Year INTEGER,
                    Genre TEXT,
                    Rating REAL,
                    Director TEXT,
                    Cast TEXT,
                    PosterUrl TEXT,
                    BackdropUrl TEXT,
                    Runtime INTEGER,
                    Season INTEGER,
                    Episode INTEGER,
                    SeriesName TEXT,
                    Artist TEXT,
                    Album TEXT,
                    Track INTEGER,
                    Duration INTEGER
                );
                
                CREATE INDEX IF NOT EXISTS idx_filepath ON MediaItems(FilePath);
                CREATE INDEX IF NOT EXISTS idx_type ON MediaItems(Type);
                CREATE INDEX IF NOT EXISTS idx_title ON MediaItems(Title);
                CREATE INDEX IF NOT EXISTS idx_artist ON MediaItems(Artist);
                CREATE INDEX IF NOT EXISTS idx_series ON MediaItems(SeriesName);
            ";
            
            using var command = new SQLiteCommand(createTableSql, connection);
            await command.ExecuteNonQueryAsync();
            
            _logger.LogInformation("Database initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing database");
            throw;
        }
    }

    public async Task<List<MediaItem>> GetAllMediaItemsAsync()
    {
        var mediaItems = new List<MediaItem>();
        
        try
        {
            using var connection = new SQLiteConnection(_connectionString);
            await connection.OpenAsync();
            
            var sql = "SELECT * FROM MediaItems ORDER BY Title";
            using var command = new SQLiteCommand(sql, connection);
            using var reader = await command.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                mediaItems.Add(MapReaderToMediaItem(reader));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all media items");
        }
        
        return mediaItems;
    }

    public async Task<MediaItem?> GetMediaItemByPathAsync(string filePath)
    {
        try
        {
            using var connection = new SQLiteConnection(_connectionString);
            await connection.OpenAsync();
            
            var sql = "SELECT * FROM MediaItems WHERE FilePath = @filePath";
            using var command = new SQLiteCommand(sql, connection);
            command.Parameters.AddWithValue("@filePath", filePath);
            
            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return MapReaderToMediaItem(reader);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting media item by path: {filePath}");
        }
        
        return null;
    }

    public async Task<int> SaveMediaItemAsync(MediaItem mediaItem)
    {
        try
        {
            using var connection = new SQLiteConnection(_connectionString);
            await connection.OpenAsync();
            
            var sql = @"
                INSERT INTO MediaItems (
                    Title, OriginalFileName, FilePath, FileSize, DateModified, Type,
                    ImdbId, TmdbId, Description, Year, Genre, Rating, Director, Cast,
                    PosterUrl, BackdropUrl, Runtime, Season, Episode, SeriesName,
                    Artist, Album, Track, Duration
                ) VALUES (
                    @Title, @OriginalFileName, @FilePath, @FileSize, @DateModified, @Type,
                    @ImdbId, @TmdbId, @Description, @Year, @Genre, @Rating, @Director, @Cast,
                    @PosterUrl, @BackdropUrl, @Runtime, @Season, @Episode, @SeriesName,
                    @Artist, @Album, @Track, @Duration
                );
                SELECT last_insert_rowid();
            ";
            
            using var command = new SQLiteCommand(sql, connection);
            AddMediaItemParameters(command, mediaItem);
            
            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error saving media item: {mediaItem.Title}");
            throw;
        }
    }

    public async Task UpdateMediaItemAsync(MediaItem mediaItem)
    {
        try
        {
            using var connection = new SQLiteConnection(_connectionString);
            await connection.OpenAsync();
            
            var sql = @"
                UPDATE MediaItems SET
                    Title = @Title, OriginalFileName = @OriginalFileName, FileSize = @FileSize,
                    DateModified = @DateModified, Type = @Type, ImdbId = @ImdbId, TmdbId = @TmdbId,
                    Description = @Description, Year = @Year, Genre = @Genre, Rating = @Rating,
                    Director = @Director, Cast = @Cast, PosterUrl = @PosterUrl, BackdropUrl = @BackdropUrl,
                    Runtime = @Runtime, Season = @Season, Episode = @Episode, SeriesName = @SeriesName,
                    Artist = @Artist, Album = @Album, Track = @Track, Duration = @Duration
                WHERE Id = @Id
            ";
            
            using var command = new SQLiteCommand(sql, connection);
            AddMediaItemParameters(command, mediaItem);
            command.Parameters.AddWithValue("@Id", mediaItem.Id);
            
            await command.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error updating media item: {mediaItem.Title}");
            throw;
        }
    }

    public async Task DeleteMediaItemAsync(int id)
    {
        try
        {
            using var connection = new SQLiteConnection(_connectionString);
            await connection.OpenAsync();
            
            var sql = "DELETE FROM MediaItems WHERE Id = @id";
            using var command = new SQLiteCommand(sql, connection);
            command.Parameters.AddWithValue("@id", id);
            
            await command.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error deleting media item with ID: {id}");
        }
    }

    public async Task DeleteMediaItemsByDriveAsync(string drivePath)
    {
        try
        {
            using var connection = new SQLiteConnection(_connectionString);
            await connection.OpenAsync();
            
            var sql = "DELETE FROM MediaItems WHERE FilePath LIKE @drivePath";
            using var command = new SQLiteCommand(sql, connection);
            command.Parameters.AddWithValue("@drivePath", $"{drivePath}%");
            
            await command.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error deleting media items for drive: {drivePath}");
        }
    }

    public async Task<List<MediaItem>> SearchMediaItemsAsync(string searchTerm)
    {
        var mediaItems = new List<MediaItem>();
        
        try
        {
            using var connection = new SQLiteConnection(_connectionString);
            await connection.OpenAsync();
            
            var sql = @"
                SELECT * FROM MediaItems 
                WHERE Title LIKE @searchTerm 
                   OR Artist LIKE @searchTerm 
                   OR Album LIKE @searchTerm 
                   OR SeriesName LIKE @searchTerm 
                   OR Director LIKE @searchTerm
                   OR Cast LIKE @searchTerm
                   OR Genre LIKE @searchTerm
                ORDER BY Title
            ";
            
            using var command = new SQLiteCommand(sql, connection);
            command.Parameters.AddWithValue("@searchTerm", $"%{searchTerm}%");
            
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                mediaItems.Add(MapReaderToMediaItem(reader));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error searching media items: {searchTerm}");
        }
        
        return mediaItems;
    }

    public async Task<List<MediaItem>> GetMediaItemsByTypeAsync(MediaType mediaType)
    {
        var mediaItems = new List<MediaItem>();
        
        try
        {
            using var connection = new SQLiteConnection(_connectionString);
            await connection.OpenAsync();
            
            var sql = "SELECT * FROM MediaItems WHERE Type = @type ORDER BY Title";
            using var command = new SQLiteCommand(sql, connection);
            command.Parameters.AddWithValue("@type", (int)mediaType);
            
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                mediaItems.Add(MapReaderToMediaItem(reader));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting media items by type: {mediaType}");
        }
        
        return mediaItems;
    }

    private void AddMediaItemParameters(SQLiteCommand command, MediaItem mediaItem)
    {
        command.Parameters.AddWithValue("@Title", mediaItem.Title);
        command.Parameters.AddWithValue("@OriginalFileName", mediaItem.OriginalFileName ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@FilePath", mediaItem.FilePath);
        command.Parameters.AddWithValue("@FileSize", mediaItem.FileSize);
        command.Parameters.AddWithValue("@DateModified", mediaItem.DateModified ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Type", (int)mediaItem.Type);
        command.Parameters.AddWithValue("@ImdbId", mediaItem.ImdbId ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@TmdbId", mediaItem.TmdbId ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Description", mediaItem.Description ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Year", mediaItem.Year ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Genre", mediaItem.Genre ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Rating", mediaItem.Rating ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Director", mediaItem.Director ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Cast", mediaItem.Cast ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@PosterUrl", mediaItem.PosterUrl ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@BackdropUrl", mediaItem.BackdropUrl ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Runtime", mediaItem.Runtime ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Season", mediaItem.Season ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Episode", mediaItem.Episode ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@SeriesName", mediaItem.SeriesName ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Artist", mediaItem.Artist ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Album", mediaItem.Album ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Track", mediaItem.Track ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@Duration", mediaItem.Duration ?? (object)DBNull.Value);
    }

    private MediaItem MapReaderToMediaItem(IDataReader reader)
    {
        return new MediaItem
        {
            Id = reader.GetInt32("Id"),
            Title = reader.GetString("Title"),
            OriginalFileName = reader.IsDBNull("OriginalFileName") ? null : reader.GetString("OriginalFileName"),
            FilePath = reader.GetString("FilePath"),
            FileSize = reader.GetInt64("FileSize"),
            DateAdded = reader.GetDateTime("DateAdded"),
            DateModified = reader.IsDBNull("DateModified") ? null : reader.GetDateTime("DateModified"),
            Type = (MediaType)reader.GetInt32("Type"),
            ImdbId = reader.IsDBNull("ImdbId") ? null : reader.GetString("ImdbId"),
            TmdbId = reader.IsDBNull("TmdbId") ? null : reader.GetString("TmdbId"),
            Description = reader.IsDBNull("Description") ? null : reader.GetString("Description"),
            Year = reader.IsDBNull("Year") ? null : reader.GetInt32("Year"),
            Genre = reader.IsDBNull("Genre") ? null : reader.GetString("Genre"),
            Rating = reader.IsDBNull("Rating") ? null : reader.GetDouble("Rating"),
            Director = reader.IsDBNull("Director") ? null : reader.GetString("Director"),
            Cast = reader.IsDBNull("Cast") ? null : reader.GetString("Cast"),
            PosterUrl = reader.IsDBNull("PosterUrl") ? null : reader.GetString("PosterUrl"),
            BackdropUrl = reader.IsDBNull("BackdropUrl") ? null : reader.GetString("BackdropUrl"),
            Runtime = reader.IsDBNull("Runtime") ? null : reader.GetInt32("Runtime"),
            Season = reader.IsDBNull("Season") ? null : reader.GetInt32("Season"),
            Episode = reader.IsDBNull("Episode") ? null : reader.GetInt32("Episode"),
            SeriesName = reader.IsDBNull("SeriesName") ? null : reader.GetString("SeriesName"),
            Artist = reader.IsDBNull("Artist") ? null : reader.GetString("Artist"),
            Album = reader.IsDBNull("Album") ? null : reader.GetString("Album"),
            Track = reader.IsDBNull("Track") ? null : reader.GetInt32("Track"),
            Duration = reader.IsDBNull("Duration") ? null : reader.GetInt32("Duration")
        };
    }
}