using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NewWinampClassic.Models;
using NewWinampClassic.Services;

namespace NewWinampClassic.ViewModels;

public partial class FavoritesViewModel : ViewModelBase
{
    private readonly IMusicLibraryService _libraryService;
    private readonly MainViewModel _mainViewModel;

    [ObservableProperty] private ObservableCollection<Song> _favoriteSongs = [];

    public FavoritesViewModel(IMusicLibraryService libraryService, MainViewModel mainViewModel)
    {
        _libraryService = libraryService;
        _mainViewModel = mainViewModel;
        Title = "Favorites";
        _libraryService.SongFavoriteToggled += OnSongFavoriteToggled;
    }

    private void OnSongFavoriteToggled(object? sender, Song song)
    {
        var existing = FavoriteSongs.FirstOrDefault(s => s.Id == song.Id);
        if (song.IsFavorite)
        {
            if (existing == null)
            {
                FavoriteSongs.Add(song);
            }
        }
        else
        {
            if (existing != null)
            {
                FavoriteSongs.Remove(existing);
            }
        }
    }

    public override async Task InitializeAsync(object? parameter = null)
    {
        IsBusy = true;
        try
        {
            var favorites = await _libraryService.GetFavoritesAsync();
            FavoriteSongs = new ObservableCollection<Song>(favorites);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task PlaySongAsync(Song song)
    {
        await _mainViewModel.PlaySongAsync(song, FavoriteSongs.ToList());
    }

    [RelayCommand]
    private async Task PlayAllAsync()
    {
        if (FavoriteSongs.Count == 0) return;
        await _mainViewModel.PlaySongAsync(FavoriteSongs[0], FavoriteSongs.ToList());
    }

    [RelayCommand]
    private async Task ToggleFavoriteAsync(Song song)
    {
        await _libraryService.ToggleFavoriteAsync(song.Id);
        song.IsFavorite = !song.IsFavorite;
        if (!song.IsFavorite)
        {
            FavoriteSongs.Remove(song);
        }
    }

    [RelayCommand]
    private async Task AddToQueueAsync(Song song) => await _mainViewModel.AddToQueueAsync(song);
}
