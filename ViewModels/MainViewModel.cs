using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using MediaScanner.Models;
using MediaScanner.Services;

namespace MediaScanner.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IDriveScanner _driveScanner;
    private readonly IMediaLibraryService _mediaLibraryService;
    private readonly ILogger<MainViewModel> _logger;
    private readonly CancellationTokenSource _cancellationTokenSource;

    [ObservableProperty]
    private ObservableCollection<NetworkDrive> _availableDrives = new();

    [ObservableProperty]
    private ObservableCollection<MediaItem> _mediaItems = new();

    [ObservableProperty]
    private ICollectionView? _filteredMediaItems;

    [ObservableProperty]
    private string _statusText = "Ready";

    [ObservableProperty]
    private bool _isScanning = false;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private bool _showMovies = true;

    [ObservableProperty]
    private bool _showTVShows = true;

    [ObservableProperty]
    private bool _showMusic = true;

    [ObservableProperty]
    private bool _showOther = true;

    [ObservableProperty]
    private string _viewMode = "Grid View";

    public ICommand ScanDrivesCommand { get; }
    public ICommand ScanDriveCommand { get; }
    public ICommand ShowDetailsCommand { get; }
    public ICommand SettingsCommand { get; }

    public MainViewModel(
        IDriveScanner driveScanner,
        IMediaLibraryService mediaLibraryService,
        ILogger<MainViewModel> logger)
    {
        _driveScanner = driveScanner;
        _mediaLibraryService = mediaLibraryService;
        _logger = logger;
        _cancellationTokenSource = new CancellationTokenSource();

        // Initialize commands
        ScanDrivesCommand = new AsyncRelayCommand(ScanForDrivesAsync);
        ScanDriveCommand = new AsyncRelayCommand<NetworkDrive>(ScanDriveAsync);
        ShowDetailsCommand = new RelayCommand<MediaItem>(ShowMediaDetails);
        SettingsCommand = new RelayCommand(ShowSettings);

        // Set up property change handlers
        PropertyChanged += OnPropertyChanged;

        // Initialize
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        try
        {
            StatusText = "Initializing...";
            
            // Initialize services
            await _mediaLibraryService.InitializeAsync();
            
            // Load existing media items
            var existingMedia = await _mediaLibraryService.GetAllMediaAsync();
            MediaItems.Clear();
            foreach (var item in existingMedia)
            {
                MediaItems.Add(item);
            }
            
            // Set up filtered view
            FilteredMediaItems = CollectionViewSource.GetDefaultView(MediaItems);
            FilteredMediaItems.Filter = MediaItemFilter;
            
            // Scan for drives
            await ScanForDrivesAsync();
            
            StatusText = $"Ready - {MediaItems.Count} media items in library";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during initialization");
            StatusText = "Error during initialization";
        }
    }

    private async Task ScanForDrivesAsync()
    {
        try
        {
            IsScanning = true;
            StatusText = "Scanning for drives...";
            
            var drives = await _driveScanner.ScanForNetworkDrivesAsync();
            
            AvailableDrives.Clear();
            foreach (var drive in drives)
            {
                AvailableDrives.Add(drive);
            }
            
            StatusText = $"Found {drives.Count} available drives";
            _logger.LogInformation($"Found {drives.Count} drives");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scanning for drives");
            StatusText = "Error scanning for drives";
        }
        finally
        {
            IsScanning = false;
        }
    }

    private async Task ScanDriveAsync(NetworkDrive? drive)
    {
        if (drive == null) return;

        try
        {
            IsScanning = true;
            
            var progress = new Progress<string>(message =>
            {
                StatusText = message;
            });
            
            await _mediaLibraryService.ScanAndUpdateLibraryAsync(drive, progress, _cancellationTokenSource.Token);
            
            // Refresh media items
            var updatedMedia = await _mediaLibraryService.GetAllMediaAsync();
            MediaItems.Clear();
            foreach (var item in updatedMedia)
            {
                MediaItems.Add(item);
            }
            
            FilteredMediaItems?.Refresh();
            
            StatusText = $"Scan complete - {MediaItems.Count} media items in library";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error scanning drive: {drive.Name}");
            StatusText = $"Error scanning {drive.Name}";
        }
        finally
        {
            IsScanning = false;
        }
    }

    private void ShowMediaDetails(MediaItem? mediaItem)
    {
        if (mediaItem == null) return;

        // TODO: Implement details dialog
        _logger.LogInformation($"Showing details for: {mediaItem.Title}");
        StatusText = $"Selected: {mediaItem.Title}";
    }

    private void ShowSettings()
    {
        // TODO: Implement settings dialog
        _logger.LogInformation("Opening settings");
        StatusText = "Settings opened";
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(SearchText):
            case nameof(ShowMovies):
            case nameof(ShowTVShows):
            case nameof(ShowMusic):
            case nameof(ShowOther):
                FilteredMediaItems?.Refresh();
                break;
        }
    }

    private bool MediaItemFilter(object obj)
    {
        if (obj is not MediaItem mediaItem)
            return false;

        // Filter by type
        var typeVisible = mediaItem.Type switch
        {
            MediaType.Movie => ShowMovies,
            MediaType.TVShow => ShowTVShows,
            MediaType.Music => ShowMusic,
            MediaType.Other => ShowOther,
            _ => false
        };

        if (!typeVisible)
            return false;

        // Filter by search text
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var searchLower = SearchText.ToLowerInvariant();
            
            var matchesTitle = mediaItem.Title.Contains(searchLower, StringComparison.OrdinalIgnoreCase);
            var matchesArtist = !string.IsNullOrEmpty(mediaItem.Artist) && 
                               mediaItem.Artist.Contains(searchLower, StringComparison.OrdinalIgnoreCase);
            var matchesAlbum = !string.IsNullOrEmpty(mediaItem.Album) && 
                              mediaItem.Album.Contains(searchLower, StringComparison.OrdinalIgnoreCase);
            var matchesSeries = !string.IsNullOrEmpty(mediaItem.SeriesName) && 
                               mediaItem.SeriesName.Contains(searchLower, StringComparison.OrdinalIgnoreCase);
            var matchesDirector = !string.IsNullOrEmpty(mediaItem.Director) && 
                                 mediaItem.Director.Contains(searchLower, StringComparison.OrdinalIgnoreCase);
            var matchesGenre = !string.IsNullOrEmpty(mediaItem.Genre) && 
                              mediaItem.Genre.Contains(searchLower, StringComparison.OrdinalIgnoreCase);

            return matchesTitle || matchesArtist || matchesAlbum || matchesSeries || matchesDirector || matchesGenre;
        }

        return true;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
        }
        base.Dispose(disposing);
    }
}