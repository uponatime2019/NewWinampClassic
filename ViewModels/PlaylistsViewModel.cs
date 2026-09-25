using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NewWinampClassic.Models;
using NewWinampClassic.Services;

namespace NewWinampClassic.ViewModels;

public partial class PlaylistsViewModel : ViewModelBase
{
    private readonly IMusicLibraryService _libraryService;
    private readonly MainViewModel _mainViewModel;

    [ObservableProperty] private ObservableCollection<Playlist> _playlists = [];
    [ObservableProperty] private ObservableCollection<Song> _selectedPlaylistSongs = [];
    [ObservableProperty] private string _selectedPlaylistName = string.Empty;
    [ObservableProperty] private bool _isPlaylistSelected;
    [ObservableProperty] private string _newPlaylistName = string.Empty;
    [ObservableProperty] private bool _isCreateDialogOpen;
    [ObservableProperty] private bool _isBusy;

    public PlaylistsViewModel(IMusicLibraryService libraryService, MainViewModel mainViewModel)
    {
        _libraryService = libraryService;
        _mainViewModel = mainViewModel;
        Title = "Playlists";

        _libraryService.SongFavoriteToggled += (s, song) =>
        {
            // Refresh Favorites playlist when a song's favorite status changes
            Task.Run(async () =>
            {
                if (SelectedPlaylistName == "Favorites")
                {
                    var favs = await _libraryService.GetFavoritesAsync();
                    SelectedPlaylistSongs = new ObservableCollection<Song>(favs);
                }
            });
        };
    }

    public override async Task InitializeAsync(object? parameter = null)
    {
        await LoadSmartPlaylistsAsync();
    }

    private ObservableCollection<Playlist> LoadSmartPlaylists()
    {
        return new ObservableCollection<Playlist>
        {
            new Playlist { Id = -1, Name = "Favorites", IsSmartPlaylist = true, SmartQuery = "favorites" },
            new Playlist { Id = -2, Name = "Recent", IsSmartPlaylist = true, SmartQuery = "recent" }
        };
    }

    private async Task LoadSmartPlaylistsAsync()
    {
        IsBusy = true;
        try
        {
            // Load smart playlists
            var smartPlaylists = LoadSmartPlaylists();

            // Load user playlists from library (placeholder — extends when playlist repo is wired)
            var albums = await _libraryService.GetAlbumsAsync();
            var userPlaylists = new ObservableCollection<Playlist>();

            Playlists = new ObservableCollection<Playlist>(smartPlaylists.Concat(userPlaylists));
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task SelectPlaylistAsync(Playlist? playlist)
    {
        if (playlist == null) return;

        SelectedPlaylistName = playlist.Name;
        IsPlaylistSelected = true;

        if (playlist.IsSmartPlaylist)
        {
            await LoadSmartPlaylistAsync(playlist.SmartQuery);
        }
        else
        {
            SelectedPlaylistSongs = [];
        }
    }

    private async Task LoadSmartPlaylistAsync(string query)
    {
        IsBusy = true;
        try
        {
            List<Song> songs = query switch
            {
                "favorites" => (await _libraryService.GetFavoritesAsync()).ToList(),
                "recent" => (await _libraryService.GetRecentSongsAsync(100)).ToList(),
                _ => []
            };
            SelectedPlaylistSongs = new ObservableCollection<Song>(songs);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void ShowCreateDialog() => IsCreateDialogOpen = true;

    [RelayCommand]
    private void CancelCreate() => IsCreateDialogOpen = false;

    [RelayCommand]
    private async Task CreatePlaylistAsync()
    {
        if (string.IsNullOrWhiteSpace(NewPlaylistName)) return;
        // Playlist creation would go through a playlist repository
        NewPlaylistName = string.Empty;
        IsCreateDialogOpen = false;
        await LoadSmartPlaylistsAsync();
    }

    [RelayCommand]
    private async Task PlaySongAsync(Song? song)
    {
        if (song == null) return;
        await _mainViewModel.PlaySongAsync(song, SelectedPlaylistSongs.ToList());
    }

    [RelayCommand]
    private async Task PlayAllAsync()
    {
        if (SelectedPlaylistSongs.Count == 0) return;
        await _mainViewModel.PlaySongAsync(SelectedPlaylistSongs[0], SelectedPlaylistSongs.ToList());
    }

    [RelayCommand]
    private async Task AddToQueueAsync(Song? song)
    {
        if (song == null) return;
        await _mainViewModel.AddToQueueAsync(song);
    }
}
