using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NewWinampClassic.Models;
using NewWinampClassic.Services;

namespace NewWinampClassic.ViewModels;

public partial class LibraryViewModel : ViewModelBase
{
    private readonly IMusicLibraryService _libraryService;
    private readonly MainViewModel _mainViewModel;

    [ObservableProperty] private ObservableCollection<Song> _songs = [];
    [ObservableProperty] private string _searchQuery = string.Empty;
    [ObservableProperty] private SortField _sortField = SortField.Title;
    [ObservableProperty] private bool _sortDescending;
    [ObservableProperty] private int _totalCount;
    [ObservableProperty] private string _filterText = "All Songs";

    public LibraryViewModel(IMusicLibraryService libraryService, MainViewModel mainViewModel)
    {
        _libraryService = libraryService;
        _mainViewModel = mainViewModel;
        Title = "Library";
        _libraryService.SongFavoriteToggled += (s, song) =>
        {
            var existing = Songs.FirstOrDefault(x => x.Id == song.Id);
            if (existing != null) existing.IsFavorite = song.IsFavorite;
        };
    }

    public override async Task InitializeAsync(object? parameter = null)
    {
        await LoadSongsAsync();
    }

    [RelayCommand]
    private async Task RefreshAsync() => await LoadSongsAsync();

    [RelayCommand]
    private async Task SearchAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchQuery))
        {
            await LoadSongsAsync();
            return;
        }

        IsBusy = true;
        try
        {
            var results = await _libraryService.SearchSongsAsync(SearchQuery);
            Songs = new ObservableCollection<Song>(results);
            TotalCount = results.Count;
            FilterText = $"Search: \"{SearchQuery}\"";
        }
        finally
        {
            IsBusy = false;
        }
    }

    partial void OnSortFieldChanged(SortField value) => _ = LoadSongsAsync();
    partial void OnSortDescendingChanged(bool value) => _ = LoadSongsAsync();

    private async Task LoadSongsAsync()
    {
        IsBusy = true;
        try
        {
            var songs = await _libraryService.GetAllSongsAsync();
            var sorted = SortField switch
            {
                SortField.Title => SortDescending ? songs.OrderByDescending(s => s.Title) : songs.OrderBy(s => s.Title),
                SortField.Artist => SortDescending ? songs.OrderByDescending(s => s.Artist) : songs.OrderBy(s => s.Artist),
                SortField.Album => SortDescending ? songs.OrderByDescending(s => s.Album) : songs.OrderBy(s => s.Album),
                SortField.Duration => SortDescending ? songs.OrderByDescending(s => s.Duration) : songs.OrderBy(s => s.Duration),
                SortField.DateAdded => SortDescending ? songs.OrderByDescending(s => s.DateAdded) : songs.OrderBy(s => s.DateAdded),
                SortField.PlayCount => SortDescending ? songs.OrderByDescending(s => s.PlayCount) : songs.OrderBy(s => s.PlayCount),
                _ => songs.OrderBy(s => s.Title)
            };
            Songs = new ObservableCollection<Song>(sorted);
            TotalCount = Songs.Count;
            FilterText = "All Songs";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task PlaySongAsync(Song song)
    {
        var list = Songs.ToList();
        await _mainViewModel.PlaySongAsync(song, list);
    }

    [RelayCommand]
    private async Task AddToQueueAsync(Song song) => await _mainViewModel.AddToQueueAsync(song);

    [RelayCommand]
    private async Task ToggleFavoriteAsync(Song song)
    {
        await _libraryService.ToggleFavoriteAsync(song.Id);
        song.IsFavorite = !song.IsFavorite;
    }
}
