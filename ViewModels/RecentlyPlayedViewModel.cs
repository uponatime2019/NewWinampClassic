using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NewWinampClassic.Models;
using NewWinampClassic.Services;

namespace NewWinampClassic.ViewModels;

public partial class RecentlyPlayedViewModel : ViewModelBase
{
    private readonly IMusicLibraryService _libraryService;
    private readonly MainViewModel _mainViewModel;

    [ObservableProperty] private ObservableCollection<Song> _recentSongs = [];

    public RecentlyPlayedViewModel(IMusicLibraryService libraryService, MainViewModel mainViewModel)
    {
        _libraryService = libraryService;
        _mainViewModel = mainViewModel;
        Title = "Recently Played";
        _libraryService.SongFavoriteToggled += (s, song) =>
        {
            var existing = RecentSongs.FirstOrDefault(x => x.Id == song.Id);
            if (existing != null) existing.IsFavorite = song.IsFavorite;
        };
    }

    public override async Task InitializeAsync(object? parameter = null)
    {
        IsBusy = true;
        try
        {
            var songs = await _libraryService.GetRecentSongsAsync(100);
            RecentSongs = new ObservableCollection<Song>(songs);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task PlaySongAsync(Song song)
    {
        await _mainViewModel.PlaySongAsync(song, RecentSongs.ToList());
    }

    [RelayCommand]
    private async Task PlayAllAsync()
    {
        if (RecentSongs.Count == 0) return;
        await _mainViewModel.PlaySongAsync(RecentSongs[0], RecentSongs.ToList());
    }

    [RelayCommand]
    private async Task AddToQueueAsync(Song song) => await _mainViewModel.AddToQueueAsync(song);
}
