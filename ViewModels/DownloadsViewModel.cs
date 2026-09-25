using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using NewWinampClassic.Models;
using NewWinampClassic.Services;

namespace NewWinampClassic.ViewModels;

public partial class DownloadsViewModel : ViewModelBase
{
    private const int MaxRecentSearches = 10;

    private readonly ISoundCloudService _soundCloudService;
    private readonly ISettingsService _settingsService;
    private readonly IMusicLibraryService _musicLibraryService;
    private readonly MainViewModel _mainViewModel;
    private readonly IQueueService _queueService;
    private readonly ILogger<DownloadsViewModel> _logger;

    [ObservableProperty] private ObservableCollection<SoundCloudTrack> _searchResults = [];
    [ObservableProperty] private ObservableCollection<string> _recentSearches = [];
    [ObservableProperty] private string _searchQuery = string.Empty;
    [ObservableProperty] private bool _isSearching;
    [ObservableProperty] private bool _isDownloading;
    [ObservableProperty] private string _downloadProgressText = string.Empty;
    [ObservableProperty] private double _downloadProgress;
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private bool _isBusy;

    public DownloadsViewModel(
        ISoundCloudService soundCloudService,
        ISettingsService settingsService,
        IMusicLibraryService musicLibraryService,
        MainViewModel mainViewModel,
        IQueueService queueService,
        ILogger<DownloadsViewModel> logger)
    {
        _soundCloudService = soundCloudService;
        _settingsService = settingsService;
        _musicLibraryService = musicLibraryService;
        _mainViewModel = mainViewModel;
        _queueService = queueService;
        _logger = logger;
        Title = "Downloads";

        LoadRecentSearches();
    }

    private void LoadRecentSearches()
    {
        var raw = _settingsService.Settings.RecentSearches;
        if (!string.IsNullOrWhiteSpace(raw))
        {
            var items = raw.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            RecentSearches = new ObservableCollection<string>(items);
        }
    }

    private void SaveRecentSearches()
    {
        _settingsService.Settings.RecentSearches = string.Join(";", RecentSearches);
        _ = _settingsService.SaveAsync();
    }

    private void AddRecentSearch(string query)
    {
        query = query.Trim();
        if (string.IsNullOrWhiteSpace(query))
            return;

        RecentSearches.Remove(query);
        RecentSearches.Insert(0, query);

        while (RecentSearches.Count > MaxRecentSearches)
            RecentSearches.RemoveAt(RecentSearches.Count - 1);

        SaveRecentSearches();
    }

    public void RemoveRecentSearch(string query)
    {
        RecentSearches.Remove(query);
        SaveRecentSearches();
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchQuery))
            return;

        IsSearching = true;
        StatusMessage = "Searching...";
        SearchResults.Clear();

        try
        {
            var results = await _soundCloudService.SearchAsync(SearchQuery);
            foreach (var track in results)
                SearchResults.Add(track);

            StatusMessage = results.Count > 0
                ? $"Found {results.Count} results"
                : "No results found";

            if (results.Count > 0)
                AddRecentSearch(SearchQuery);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Search failed");
            StatusMessage = $"Search failed: {ex.Message}";
        }
        finally
        {
            IsSearching = false;
        }
    }

    [RelayCommand]
    private async Task PlayTrackAsync(SoundCloudTrack? track)
    {
        if (track == null)
            return;

        IsBusy = true;
        StatusMessage = $"Loading {track.Title}...";

        try
        {
            var streamUrl = await _soundCloudService.GetStreamUrlAsync(track);

            var song = new Song
            {
                FilePath = streamUrl,
                Title = track.Title,
                Artist = track.DisplayArtist,
                ArtworkPath = track.Thumbnail
            };

            var queue = _queueService.Queue;
            var existingIndex = -1;
            for (int i = 0; i < queue.Count; i++)
            {
                if (string.Equals(queue[i].FilePath, song.FilePath, StringComparison.OrdinalIgnoreCase))
                {
                    existingIndex = i;
                    break;
                }
            }

            if (existingIndex >= 0)
            {
                _queueService.PlayAt(existingIndex);
                await _mainViewModel.PlaySongAsync(queue[existingIndex]);
            }
            else
            {
                _queueService.AddToQueue(song);
                _queueService.PlayAt(queue.Count - 1);
                await _mainViewModel.PlaySongAsync(song);
            }

            StatusMessage = $"▶ Playing: {track.Title}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stream '{Title}'", track.Title);
            StatusMessage = $"✗ Playback failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task DownloadAsync(SoundCloudTrack? track)
    {
        if (track == null)
            return;

        var libraryPath = _settingsService.Settings.LibraryPath;
        if (string.IsNullOrWhiteSpace(libraryPath))
        {
            StatusMessage = "Please set a music library folder in Settings first.";
            return;
        }

        // LibraryPath may be semicolon-separated; use the first folder
        var firstFolder = libraryPath.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)[0];
        var downloadsFolder = Path.Combine(firstFolder, "WinampDownloads");
        Directory.CreateDirectory(downloadsFolder);

        IsDownloading = true;
        DownloadProgress = 0;
        DownloadProgressText = $"Downloading {track.Title}...";
        StatusMessage = DownloadProgressText;

        var progress = new Progress<double>(p =>
        {
            DownloadProgress = p;
            DownloadProgressText = p < 100
                ? $"Downloading {track.Title}... {p:F0}%"
                : $"Saved: {track.SafeFileName}.mp3";
        });

        try
        {
            await _soundCloudService.DownloadAsync(track, downloadsFolder, progress);
            StatusMessage = $"✓ Downloaded to WinampDownloads/{track.SafeFileName}.mp3 — Refreshing library...";

            // Rescan the downloads folder so the song appears in library
            try
            {
                await _musicLibraryService.ScanLibraryAsync(downloadsFolder);
                StatusMessage = $"✓ Downloaded and added to library!";
            }
            catch (Exception scanEx)
            {
                _logger.LogWarning(scanEx, "Library rescan after download failed");
                StatusMessage = $"✓ Downloaded to WinampDownloads (library rescan failed)";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Download failed for '{Title}'", track.Title);
            StatusMessage = $"✗ Download failed: {ex.Message}";
        }
        finally
        {
            IsDownloading = false;
            DownloadProgress = 0;
            DownloadProgressText = string.Empty;
        }
    }
}
