using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NewWinampClassic.Models;
using NewWinampClassic.Services;

namespace NewWinampClassic.ViewModels;

public partial class SearchViewModel : ViewModelBase
{
    private readonly IMusicLibraryService _libraryService;
    private readonly MainViewModel _mainViewModel;
    private readonly IQueueService _queueService;

    [ObservableProperty] private string _searchQuery = string.Empty;
    [ObservableProperty] private ObservableCollection<Song> _results = [];
    [ObservableProperty] private ObservableCollection<Album> _albumResults = [];
    [ObservableProperty] private ObservableCollection<Artist> _artistResults = [];
    [ObservableProperty] private bool _hasSearched;
    [ObservableProperty] private bool _hasResults;

    public SearchViewModel(IMusicLibraryService libraryService, MainViewModel mainViewModel, IQueueService queueService)
    {
        _libraryService = libraryService;
        _mainViewModel = mainViewModel;
        _queueService = queueService;
        Title = "Search";
        _libraryService.SongFavoriteToggled += (s, song) =>
        {
            var existing = Results.FirstOrDefault(x => x.Id == song.Id);
            if (existing != null) existing.IsFavorite = song.IsFavorite;
        };
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchQuery)) return;

        IsBusy = true;
        HasSearched = true;
        try
        {
            var songs = await _libraryService.SearchSongsAsync(SearchQuery);
            Results = new ObservableCollection<Song>(songs);

            var albums = await _libraryService.GetAlbumsAsync();
            AlbumResults = new ObservableCollection<Album>(
                albums.Where(a => a.Name.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase)));

            var artists = await _libraryService.GetArtistsAsync();
            ArtistResults = new ObservableCollection<Artist>(
                artists.Where(a => a.Name.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase)));

            HasResults = Results.Count > 0 || AlbumResults.Count > 0 || ArtistResults.Count > 0;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task PlaySongAsync(Song song)
    {
        var queue = _queueService.Queue;
        var existingIndex = -1;
        for (int i = 0; i < queue.Count; i++)
        {
            if (queue[i].Id == song.Id ||
                (!string.IsNullOrEmpty(song.FilePath) && string.Equals(queue[i].FilePath, song.FilePath, StringComparison.OrdinalIgnoreCase)))
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
    }

    [RelayCommand]
    private async Task AddToQueueAsync(Song song) => await _mainViewModel.AddToQueueAsync(song);

    [RelayCommand]
    private async Task PlayAlbumAsync(Album album)
    {
        var songs = await _libraryService.GetSongsByAlbumAsync(album.Name);
        var list = songs.ToList();
        if (list.Count > 0)
            await _mainViewModel.PlaySongAsync(list[0], list);
    }

    [RelayCommand]
    private async Task PlayArtistAsync(Artist artist)
    {
        var songs = await _libraryService.GetSongsByArtistAsync(artist.Id);
        var list = songs.ToList();
        if (list.Count > 0)
            await _mainViewModel.PlaySongAsync(list[0], list);
    }
}
