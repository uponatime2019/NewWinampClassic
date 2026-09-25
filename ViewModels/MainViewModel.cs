using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using NewWinampClassic.Models;
using NewWinampClassic.Services;

namespace NewWinampClassic.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IAudioPlaybackService _playbackService;
    private readonly IQueueService _queueService;
    private readonly ISettingsService _settingsService;

    private bool _isStopAndSaveCompleted;

    [ObservableProperty] private bool _isShuttingDown;

    [ObservableProperty] private Song? _currentSong;
    private Song? _previousSong;
    [ObservableProperty] private PlaybackState _playbackState = PlaybackState.Stopped;
    [ObservableProperty] private TimeSpan _currentPosition;
    [ObservableProperty] private TimeSpan _duration;
    [ObservableProperty] private double _volume = 1.0;
    [ObservableProperty] private bool _isMuted;
    [ObservableProperty] private double _playbackSpeed = 1.0;
    [ObservableProperty] private bool _shuffleEnabled;
    [ObservableProperty] private RepeatMode _repeatMode;
    [ObservableProperty] private string _searchQuery = string.Empty;
    [ObservableProperty] private bool _isPlayerBarVisible;

    public ObservableCollection<Song> SearchResults { get; } = [];

    public MainViewModel(
        IAudioPlaybackService playbackService,
        IQueueService queueService,
        ISettingsService settingsService)
    {
        _playbackService = playbackService;
        _queueService = queueService;
        _settingsService = settingsService;

        _playbackService.PlaybackStateChanged += OnPlaybackStateChanged;
        _playbackService.PositionChanged += OnPositionChanged;
        _playbackService.SongChanged += OnSongChanged;
        _playbackService.SongEnded += OnSongEnded;

        _queueService.CurrentSongChanged += OnQueueCurrentSongChanged;

        var settings = _settingsService.Settings;
        _volume = settings.Volume;
        _isMuted = settings.IsMuted;
        _shuffleEnabled = settings.ShuffleEnabled;
        _repeatMode = settings.RepeatMode;
        _playbackSpeed = settings.PlaybackSpeed;
    }

    private void OnPlaybackStateChanged(object? sender, PlaybackStateChangedEventArgs e)
    {
        if (IsShuttingDown) return;
        EnqueueOnUI(() => PlaybackState = e.NewState);
    }

    private void OnPositionChanged(object? sender, PositionChangedEventArgs e)
    {
        if (IsShuttingDown) return;
        EnqueueOnUI(() =>
        {
            CurrentPosition = e.Position;
            Duration = e.Duration;
        });
    }

    private void OnSongChanged(object? sender, SongChangedEventArgs e)
    {
        if (IsShuttingDown) return;
        EnqueueOnUI(() =>
        {
            CurrentSong = e.NewSong;
            IsPlayerBarVisible = e.NewSong is not null;
        });
    }

    private void OnSongEnded(object? sender, EventArgs e)
    {
        if (IsShuttingDown) return;
        EnqueueOnUI(() =>
        {
            var next = _queueService.Next();
            if (next is not null)
            {
                _ = PlayWithFallbackAsync(next);
            }
            else
            {
                PlaybackState = PlaybackState.Stopped;
            }
        });
    }

    private void OnQueueCurrentSongChanged(object? sender, EventArgs e)
    {
        if (IsShuttingDown) return;
        EnqueueOnUI(() => CurrentSong = _queueService.CurrentSong);
    }

    private static void EnqueueOnUI(Action action)
    {
        try
        {
            var window = App.MainWindow;
            if (window is not null && window.DispatcherQueue.TryEnqueue(() => action()))
                return;
        }
        catch { }
    }

    public string CurrentPositionText => $"{(int)CurrentPosition.TotalMinutes}:{CurrentPosition.Seconds:D2}";
    public string DurationText => $"{(int)Duration.TotalMinutes}:{Duration.Seconds:D2}";
    public string PlaybackSpeedText => $"{PlaybackSpeed:F1}x";

    partial void OnVolumeChanged(double value) => _playbackService.Volume = value;
    partial void OnIsMutedChanged(bool value) => _playbackService.IsMuted = value;
    partial void OnPlaybackSpeedChanged(double value) => _playbackService.PlaybackSpeed = value;

    partial void OnCurrentSongChanged(Song? value)
    {
        if (_previousSong is not null)
            _previousSong.IsNowPlaying = false;
        if (value is not null)
        {
            value.IsNowPlaying = true;
            _ = Task.Run(async () =>
            {
                try
                {
                    if (value.Id > 0)
                    {
                        var libraryService = App.Services.GetRequiredService<IMusicLibraryService>();
                        await libraryService.AddSongToHistoryAsync(value.Id);
                    }
                }
                catch { }
            });
        }
        _previousSong = value;
    }

    [RelayCommand]
    private async Task PlayPauseAsync()
    {
        if (PlaybackState == PlaybackState.Playing)
            await _playbackService.PauseAsync();
        else if (PlaybackState == PlaybackState.Paused)
            await _playbackService.ResumeAsync();
        else if (_queueService.CurrentSong is not null)
            await PlayWithFallbackAsync(_queueService.CurrentSong);
    }

    [RelayCommand]
    private async Task StopPlaybackAsync()
    {
        await _playbackService.StopAsync();
        PlaybackState = PlaybackState.Stopped;
    }

    /// <summary>
    /// Plays the song at the given index of the current queue (playlist double-click).
    /// </summary>
    public async Task PlayQueueAtAsync(int index)
    {
        if (index < 0 || index >= _queueService.Queue.Count)
            return;

        _queueService.PlayAt(index);
        if (_queueService.CurrentSong is not null)
            await PlayWithFallbackAsync(_queueService.CurrentSong);
    }

    [RelayCommand]
    private async Task NextTrackAsync()
    {
        var next = _queueService.Next();
        if (next is not null)
            await PlayWithFallbackAsync(next);
    }

    [RelayCommand]
    private async Task PreviousTrackAsync()
    {
        if (CurrentPosition > TimeSpan.FromSeconds(3))
        {
            await _playbackService.SeekAsync(TimeSpan.Zero);
            return;
        }
        var prev = _queueService.Previous();
        if (prev is not null)
            await PlayWithFallbackAsync(prev);
    }

    [RelayCommand]
    private async Task SeekAsync(double positionSeconds)
    {
        await _playbackService.SeekAsync(TimeSpan.FromSeconds(positionSeconds));
    }

    [RelayCommand]
    private void ToggleShuffle()
    {
        _queueService.IsShuffleEnabled = !_queueService.IsShuffleEnabled;
        ShuffleEnabled = _queueService.IsShuffleEnabled;
    }

    [RelayCommand]
    private void CycleRepeatMode()
    {
        _queueService.RepeatMode = _queueService.RepeatMode switch
        {
            RepeatMode.None => RepeatMode.All,
            RepeatMode.All => RepeatMode.One,
            RepeatMode.One => RepeatMode.None,
            _ => RepeatMode.None
        };
        RepeatMode = _queueService.RepeatMode;
    }

    public async Task PlaySongAsync(Song song, IReadOnlyList<Song>? queue = null)
    {
        if (queue is not null)
            _queueService.SetQueue(queue, queue.ToList().IndexOf(song));

        await PlayWithFallbackAsync(song);
    }

    /// <summary>
    /// Attempts to play a song; if it fails, auto-skips to the next song in the queue.
    /// </summary>
    private async Task PlayWithFallbackAsync(Song song)
    {
        try
        {
            await _playbackService.PlayAsync(song);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Play failed for \"{song.Title}\": {ex.Message} — trying next song");
            var next = _queueService.Next();
            if (next is not null)
                await PlayWithFallbackAsync(next);
        }
    }

    public async Task PlayFilePathAsync(string filePath)
    {
        try
        {
            var libraryService = App.Services.GetRequiredService<IMusicLibraryService>();
            var song = await libraryService.AddSongAsync(filePath);
            if (song is not null)
            {
                await PlaySongAsync(song, new List<Song> { song });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to play file from arguments: {ex.Message}");
        }
    }

    public async Task AddToQueueAsync(Song song)
    {
        _queueService.AddToQueue(song);
    }

    public async Task SaveStateAsync()
    {
        var settings = _settingsService.Settings;
        settings.Volume = Volume;
        settings.IsMuted = IsMuted;
        settings.ShuffleEnabled = _queueService.IsShuffleEnabled;
        settings.RepeatMode = _queueService.RepeatMode;
        settings.PlaybackSpeed = PlaybackSpeed;
        if (CurrentSong is not null)
        {
            settings.LastPlayedFilePath = CurrentSong.FilePath;
            settings.LastPlaybackPosition = CurrentPosition.TotalSeconds;
        }
        await _settingsService.SaveAsync();
    }

    public async Task StopAndSaveAsync()
    {
        if (_isStopAndSaveCompleted)
            return;

        _isStopAndSaveCompleted = true;
        IsShuttingDown = true;

        _playbackService.PlaybackStateChanged -= OnPlaybackStateChanged;
        _playbackService.PositionChanged -= OnPositionChanged;
        _playbackService.SongChanged -= OnSongChanged;
        _playbackService.SongEnded -= OnSongEnded;
        _queueService.CurrentSongChanged -= OnQueueCurrentSongChanged;

        await _playbackService.StopAsync();
        _playbackService.Dispose();
        await SaveStateAsync();
    }
}
