using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NewWinampClassic.Models;
using NewWinampClassic.Services;

namespace NewWinampClassic.ViewModels;

public partial class QueueViewModel : ViewModelBase
{
    private readonly IQueueService _queueService;
    private readonly MainViewModel _mainViewModel;

    [ObservableProperty] private ObservableCollection<Song> _queueSongs = [];
    [ObservableProperty] private Song? _currentSong;
    [ObservableProperty] private int _currentIndex = -1;

    public QueueViewModel(IQueueService queueService, MainViewModel mainViewModel)
    {
        _queueService = queueService;
        _mainViewModel = mainViewModel;
        Title = "Queue";

        _queueService.QueueChanged += OnQueueChanged;
        _queueService.CurrentSongChanged += OnCurrentSongChanged;

        var libraryService = App.Services.GetService(typeof(IMusicLibraryService)) as IMusicLibraryService;
        if (libraryService != null)
        {
            libraryService.SongFavoriteToggled += (s, song) =>
            {
                var existing = QueueSongs.FirstOrDefault(x => x.Id == song.Id);
                if (existing != null) existing.IsFavorite = song.IsFavorite;
            };
        }

        RefreshQueue();
    }

    private void OnQueueChanged(object? sender, EventArgs e) =>
        App.MainWindow!.DispatcherQueue.TryEnqueue(RefreshQueue);

    private void OnCurrentSongChanged(object? sender, EventArgs e) =>
        App.MainWindow!.DispatcherQueue.TryEnqueue(() =>
        {
            CurrentSong = _queueService.CurrentSong;
            CurrentIndex = _queueService.CurrentIndex;
        });

    private void RefreshQueue()
    {
        QueueSongs = new ObservableCollection<Song>(_queueService.Queue);
        CurrentSong = _queueService.CurrentSong;
        CurrentIndex = _queueService.CurrentIndex;
    }

    [RelayCommand]
    private async Task PlaySongAsync(Song song)
    {
        var index = QueueSongs.IndexOf(song);
        if (index >= 0)
        {
            _queueService.PlayAt(index);
            if (song is not null)
                await _mainViewModel.PlaySongAsync(song, QueueSongs.ToList());
        }
    }

    [RelayCommand]
    private void RemoveSong(Song song) => _queueService.RemoveFromQueue(QueueSongs.IndexOf(song));

    [RelayCommand]
    private void ClearQueue() => _queueService.Clear();
}
