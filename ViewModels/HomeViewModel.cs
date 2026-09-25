using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NewWinampClassic.Models;
using NewWinampClassic.Services;

namespace NewWinampClassic.ViewModels;

public partial class HomeViewModel : ViewModelBase
{
    private readonly IMusicLibraryService _libraryService;
    private readonly MainViewModel _mainViewModel;

    [ObservableProperty] private ObservableCollection<Song> _recentSongs = [];
    [ObservableProperty] private ObservableCollection<Song> _mostPlayed = [];
    [ObservableProperty] private ObservableCollection<Song> _suggestedSongs = [];
    [ObservableProperty] private ObservableCollection<Song> _favorites = [];
    [ObservableProperty] private ObservableCollection<Album> _recentAlbums = [];
    [ObservableProperty] private bool _hasLibrary;
    [ObservableProperty] private int _totalSongs;
    [ObservableProperty] private int _totalAlbums;
    [ObservableProperty] private int _totalArtists;

    public HomeViewModel(IMusicLibraryService libraryService, MainViewModel mainViewModel)
    {
        _libraryService = libraryService;
        _mainViewModel = mainViewModel;
        Title = "Home";
        _libraryService.SongFavoriteToggled += (s, song) =>
        {
            var r = RecentSongs.FirstOrDefault(x => x.Id == song.Id);
            if (r != null) r.IsFavorite = song.IsFavorite;

            var m = MostPlayed.FirstOrDefault(x => x.Id == song.Id);
            if (m != null) m.IsFavorite = song.IsFavorite;

            var su = SuggestedSongs.FirstOrDefault(x => x.Id == song.Id);
            if (su != null) su.IsFavorite = song.IsFavorite;

            var f = Favorites.FirstOrDefault(x => x.Id == song.Id);
            if (f != null)
            {
                if (!song.IsFavorite) Favorites.Remove(f);
            }
            else if (song.IsFavorite && Favorites.Count < 20)
            {
                Favorites.Add(song);
            }
        };
    }

    public override async Task InitializeAsync(object? parameter = null)
    {
        IsBusy = true;
        try
        {
            await LoadDataAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        IsBusy = true;
        try
        {
            await LoadDataAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LoadDataAsync()
    {
        var recent = await _libraryService.GetRecentSongsAsync(20);
        var mostPlayed = await _libraryService.GetMostPlayedAsync(20);
        var favorites = await _libraryService.GetFavoritesAsync();
        var albums = await _libraryService.GetAlbumsAsync();
        var artists = await _libraryService.GetArtistsAsync();

        RecentSongs = new ObservableCollection<Song>(recent);
        MostPlayed = new ObservableCollection<Song>(mostPlayed);
        Favorites = new ObservableCollection<Song>(favorites.Take(20));
        RecentAlbums = new ObservableCollection<Album>(albums.OrderByDescending(a => a.DateAdded).Take(10));

        var shownSongIds = recent.Select(s => s.Id).Concat(mostPlayed.Select(s => s.Id)).ToHashSet();
        var allSongs = await _libraryService.GetAllSongsAsync();
        var candidates = allSongs.Where(s => !shownSongIds.Contains(s.Id)).ToList();
        if (recent.Count + mostPlayed.Count <= 20 && candidates.Count > 0)
        {
            var rnd = new Random();
            SuggestedSongs = new ObservableCollection<Song>(
                candidates.OrderBy(_ => rnd.Next()).Take(30));
        }
        else
        {
            SuggestedSongs = [];
        }

        TotalSongs = _libraryService.TotalSongs;
        TotalAlbums = albums.Count;
        TotalArtists = artists.Count;
        HasLibrary = TotalSongs > 0;
    }

    [RelayCommand]
    private async Task PlaySongAsync(Song song)
    {
        var allSongs = await _libraryService.GetAllSongsAsync();
        await _mainViewModel.PlaySongAsync(song, allSongs.ToList());
    }

    [RelayCommand]
    private async Task AddToQueueAsync(Song song) => await _mainViewModel.AddToQueueAsync(song);
}
