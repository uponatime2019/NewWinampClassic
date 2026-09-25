using NewWinampClassic.Models;

namespace NewWinampClassic.Services;

public interface IAudioPlaybackService : IDisposable
{
    PlaybackState State { get; }
    Song? CurrentSong { get; }
    TimeSpan CurrentPosition { get; }
    TimeSpan Duration { get; }
    double Volume { get; set; }
    bool IsMuted { get; set; }
    double PlaybackSpeed { get; set; }
    double Balance { get; set; }
    int SampleRate { get; }
    int Channels { get; }

    event EventHandler<PlaybackStateChangedEventArgs>? PlaybackStateChanged;
    event EventHandler<PositionChangedEventArgs>? PositionChanged;
    event EventHandler<SongChangedEventArgs>? SongChanged;
    event EventHandler? SongEnded;
    event EventHandler<FftEventArgs>? FftCalculated;

    Task PlayAsync(Song song, double startPosition = 0);
    Task ResumeAsync();
    Task PauseAsync();
    Task StopAsync();
    Task SeekAsync(TimeSpan position);
    void SetEqBand(int band, float gainDb);
}

public class PlaybackStateChangedEventArgs(PlaybackState newState) : EventArgs
{
    public PlaybackState NewState { get; } = newState;
}

public class PositionChangedEventArgs(TimeSpan position, TimeSpan duration) : EventArgs
{
    public TimeSpan Position { get; } = position;
    public TimeSpan Duration { get; } = duration;
}

public class SongChangedEventArgs(Song? song) : EventArgs
{
    public Song? NewSong { get; } = song;
}
