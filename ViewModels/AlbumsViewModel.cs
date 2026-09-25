using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NewWinampClassic.Models;
using NewWinampClassic.Services;

namespace NewWinampClassic.ViewModels;

public partial class AlbumsViewModel : ViewModelBase
{
    private readonly IMusicLibraryService _libraryService;
    private readonly MainViewModel _mainViewModel;

    [ObservableProperty] private ObservableCollection<Album> _albums = [];
    [ObservableProperty] private Album? _selectedAlbum;
    [ObservableProperty] private ObservableCollection<Song> _albumSongs = [];
    [ObservableProperty] private bool _isAlbumDetailVisible;

    public AlbumsViewModel(IMusicLibraryService libraryService, MainViewModel mainViewModel)
    {
        _libraryService = libraryService;
        _mainViewModel = mainViewModel;
        Title = "Albums";
        _libraryService.SongFavoriteToggled += (s, song) =>
        {
            var existing = AlbumSongs.FirstOrDefault(x => x.Id == song.Id);
            if (existing != null) existing.IsFavorite = song.IsFavorite;
        };
    }

    public override async Task InitializeAsync(object? parameter = null)
    {
        IsBusy = true;
        try
        {
            var albums = await _libraryService.GetAlbumsAsync();
            Albums = new ObservableCollection<Album>(albums.OrderBy(a => a.Name));
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task OpenAlbumAsync(Album album)
    {
        SelectedAlbum = album;
        var songs = await _libraryService.GetSongsByAlbumAsync(album.Name);
        AlbumSongs = new ObservableCollection<Song>(songs);
        IsAlbumDetailVisible = true;
    }

    [RelayCommand]
    private void CloseAlbumDetail()
    {
        IsAlbumDetailVisible = false;
        SelectedAlbum = null;
        AlbumSongs.Clear();
    }

    [RelayCommand]
    private async Task PlayAlbumAsync(Album album)
    {
        var songs = await _libraryService.GetSongsByAlbumAsync(album.Name);
        var list = songs.ToList();
        if (list.Count > 0)
            await _mainViewModel.PlaySongAsync(list[0], list);
    }

    [RelayCommand]
    private async Task PlaySongAsync(Song song)
    {
        await _mainViewModel.PlaySongAsync(song, AlbumSongs.ToList());
    }

    [RelayCommand]
    private void GoBack() => CloseAlbumDetail();
}
