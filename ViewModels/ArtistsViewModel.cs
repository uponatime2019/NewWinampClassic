using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NewWinampClassic.Models;
using NewWinampClassic.Services;

namespace NewWinampClassic.ViewModels;

public partial class ArtistsViewModel : ViewModelBase
{
    private readonly IMusicLibraryService _libraryService;
    private readonly MainViewModel _mainViewModel;

    [ObservableProperty] private ObservableCollection<Artist> _artists = [];
    [ObservableProperty] private Artist? _selectedArtist;
    [ObservableProperty] private ObservableCollection<Song> _artistSongs = [];
    [ObservableProperty] private bool _isArtistDetailVisible;

    public ArtistsViewModel(IMusicLibraryService libraryService, MainViewModel mainViewModel)
    {
        _libraryService = libraryService;
        _mainViewModel = mainViewModel;
        Title = "Artists";
        _libraryService.SongFavoriteToggled += (s, song) =>
        {
            var existing = ArtistSongs.FirstOrDefault(x => x.Id == song.Id);
            if (existing != null) existing.IsFavorite = song.IsFavorite;
        };
    }

    public override async Task InitializeAsync(object? parameter = null)
    {
        IsBusy = true;
        try
        {
            var artists = await _libraryService.GetArtistsAsync();
            Artists = new ObservableCollection<Artist>(artists.OrderBy(a => a.Name));
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task OpenArtistAsync(Artist artist)
    {
        SelectedArtist = artist;
        var songs = await _libraryService.GetSongsByArtistAsync(artist.Id);
        ArtistSongs = new ObservableCollection<Song>(songs);
        IsArtistDetailVisible = true;
    }

    [RelayCommand]
    private void CloseArtistDetail()
    {
        IsArtistDetailVisible = false;
        SelectedArtist = null;
        ArtistSongs.Clear();
    }

    [RelayCommand]
    private async Task PlaySongAsync(Song song)
    {
        await _mainViewModel.PlaySongAsync(song, ArtistSongs.ToList());
    }

    [RelayCommand]
    private void GoBack() => CloseArtistDetail();
}
