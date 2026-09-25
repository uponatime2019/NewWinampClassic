using Microsoft.Extensions.Logging;
using NAudio.Wave;
using NewWinampClassic.Models;

namespace NewWinampClassic.Services;

public sealed class AudioPlaybackService : IAudioPlaybackService
{
    private readonly ILogger<AudioPlaybackService> _logger;
    private WaveOutEvent? _waveOut;
    private WaveStream? _waveStream;
    private SampleAggregator? _sampleAggregator;
    private EqProvider? _eqProvider;
    private System.Timers.Timer _positionTimer;
    private System.Timers.Timer _fftTimer;
    private bool _isStopping;
    private bool _isDisposed;

    private Models.PlaybackState _state = Models.PlaybackState.Stopped;
    public Models.PlaybackState State
    {
        get => _state;
        private set
        {
            if (_state != value)
            {
                _state = value;
                PlaybackStateChanged?.Invoke(this, new PlaybackStateChangedEventArgs((Models.PlaybackState)value));
            }
        }
    }

    private Song? _currentSong;
    public Song? CurrentSong
    {
        get => _currentSong;
        private set
        {
            _currentSong = value;
            SongChanged?.Invoke(this, new SongChangedEventArgs(value));
        }
    }

    public TimeSpan CurrentPosition => _waveStream?.CurrentTime ?? TimeSpan.Zero;

    public TimeSpan Duration => _waveStream?.TotalTime ?? TimeSpan.Zero;

    private double _volume = 1.0;
    public double Volume
    {
        get => _volume;
        set
        {
            var clamped = Math.Clamp(value, 0.0, 1.0);
            if (_volume != clamped)
            {
                _volume = clamped;
                ApplyVolume();
            }
        }
    }

    private bool _isMuted;
    public bool IsMuted
    {
        get => _isMuted;
        set
        {
            if (_isMuted != value)
            {
                _isMuted = value;
                ApplyVolume();
            }
        }
    }

    private double _playbackSpeed = 1.0;
    public double PlaybackSpeed
    {
        get => _playbackSpeed;
        set => _playbackSpeed = Math.Clamp(value, 0.25, 4.0);
    }

    private double _balance;
    public double Balance
    {
        get => _balance;
        set
        {
            _balance = value;
            if (_eqProvider is not null)
                _eqProvider.Balance = value;
        }
    }

    public int SampleRate => _waveStream?.WaveFormat?.SampleRate ?? 0;

    public int Channels => _waveStream?.WaveFormat?.Channels ?? 0;

    public event EventHandler<PlaybackStateChangedEventArgs>? PlaybackStateChanged;
    public event EventHandler<PositionChangedEventArgs>? PositionChanged;
    public event EventHandler<SongChangedEventArgs>? SongChanged;
    public event EventHandler? SongEnded;
    public event EventHandler<FftEventArgs>? FftCalculated;

    public AudioPlaybackService(ILogger<AudioPlaybackService> logger)
    {
        _logger = logger;
        _positionTimer = new System.Timers.Timer(100);
        _positionTimer.Elapsed += OnPositionTimerElapsed;
        _fftTimer = new System.Timers.Timer(50); // ~20 FPS for visualizer
        _fftTimer.Elapsed += OnFftTimerElapsed;
    }

    public async Task PlayAsync(Song song, double startPosition = 0)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        _logger.LogInformation("Playing: {Title} from {FilePath} at position {Start}", song.Title, song.FilePath, startPosition);

        _isStopping = false;
        State = Models.PlaybackState.Loading;

        try
        {
            CleanupPlayback();

            _waveStream = new MediaFoundationReader(song.FilePath);

            // Build provider chain:
            // MediaFoundationReader → SampleAggregator (FFT) → EqProvider → WaveOutEvent
            _sampleAggregator = new SampleAggregator(_waveStream);
            _sampleAggregator.FftCalculated += OnFftCalculated;

            _eqProvider = new EqProvider(_sampleAggregator)
            {
                Balance = _balance,
            };

            _waveOut = new WaveOutEvent();
            _waveOut.PlaybackStopped += OnPlaybackStopped;
            _waveOut.Init(_eqProvider);

            CurrentSong = song;

            if (startPosition > 0 && startPosition < 1.0)
            {
                var seekPosition = TimeSpan.FromTicks((long)(_waveStream.TotalTime.Ticks * startPosition));
                _waveStream.CurrentTime = seekPosition;
            }

            ApplyVolume();
            _waveOut.Play();
            State = Models.PlaybackState.Playing;
            _positionTimer.Start();
            _fftTimer.Start();

            _logger.LogInformation("Now playing: {Title} ({Duration})", song.Title, _waveStream.TotalTime);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to play: {FilePath}", song.FilePath);
            State = Models.PlaybackState.Stopped;
            CurrentSong = null;
            throw;
        }

        await Task.CompletedTask;
    }

    private void OnFftCalculated(object? sender, FftEventArgs e)
    {
        FftCalculated?.Invoke(this, e);
    }

    private void OnFftTimerElapsed(object? sender, System.Timers.ElapsedEventArgs e)
    {
        if (_sampleAggregator is not null && State == Models.PlaybackState.Playing)
        {
            _sampleAggregator.PerformFft();
        }
    }

    public void SetEqBand(int band, float gainDb)
    {
        _eqProvider?.SetBand(band, gainDb);
    }

    public async Task ResumeAsync()
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        if (_waveOut is null || _waveStream is null)
        {
            _logger.LogWarning("Resume called but no playback is active.");
            return;
        }

        _logger.LogInformation("Resuming playback.");
        _isStopping = false;

        try
        {
            _waveOut.Play();
            State = Models.PlaybackState.Playing;
            _positionTimer.Start();
            _fftTimer.Start();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resume playback.");
        }

        await Task.CompletedTask;
    }

    public async Task PauseAsync()
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        if (_waveOut is null)
        {
            _logger.LogWarning("Pause called but no playback is active.");
            return;
        }

        _logger.LogInformation("Pausing playback.");

        try
        {
            _waveOut.Pause();
            State = Models.PlaybackState.Paused;
            _positionTimer.Stop();
            _fftTimer.Stop();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to pause playback.");
        }

        await Task.CompletedTask;
    }

    public async Task StopAsync()
    {
        if (_isDisposed || _waveOut is null)
            return;

        _logger.LogInformation("Stopping playback.");
        _isStopping = true;

        try
        {
            _positionTimer.Stop();
            _fftTimer.Stop();
            if (_waveOut is not null)
            {
                _waveOut.PlaybackStopped -= OnPlaybackStopped;
                _waveOut.Stop();
            }
            State = Models.PlaybackState.Stopped;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop playback.");
        }

        await Task.CompletedTask;
    }

    public async Task SeekAsync(TimeSpan position)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        if (_waveStream is null)
        {
            return;
        }

        _logger.LogDebug("Seeking to {Position}", position);

        try
        {
            var clamped = TimeSpan.FromTicks(
                Math.Clamp(position.Ticks, 0, _waveStream.TotalTime.Ticks));
            _waveStream.CurrentTime = clamped;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to seek to {Position}", position);
        }

        await Task.CompletedTask;
    }

    private void ApplyVolume()
    {
        if (_waveOut is not null)
        {
            _waveOut.Volume = _isMuted ? 0.0f : (float)_volume;
        }
    }

    private void OnPositionTimerElapsed(object? sender, System.Timers.ElapsedEventArgs e)
    {
        if (_waveStream is not null && State == Models.PlaybackState.Playing)
        {
            PositionChanged?.Invoke(this, new PositionChangedEventArgs(
                _waveStream.CurrentTime,
                _waveStream.TotalTime));
        }
    }

    private void OnPlaybackStopped(object? sender, StoppedEventArgs e)
    {
        _positionTimer.Stop();

        if (!_isStopping)
        {
            _logger.LogInformation("Playback reached end of song naturally.");
            State = Models.PlaybackState.Stopped;
            SongEnded?.Invoke(this, EventArgs.Empty);
        }

        _isStopping = false;
    }

    private void CleanupPlayback()
    {
        _positionTimer.Stop();
        _fftTimer.Stop();

        if (_sampleAggregator is not null)
        {
            _sampleAggregator.FftCalculated -= OnFftCalculated;
            _sampleAggregator = null;
        }

        _eqProvider = null;

        if (_waveOut is not null)
        {
            _waveOut.PlaybackStopped -= OnPlaybackStopped;
            _waveOut.Dispose();
            _waveOut = null;
        }

        if (_waveStream is not null)
        {
            _waveStream.Dispose();
            _waveStream = null;
        }
    }

    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;
        _positionTimer.Elapsed -= OnPositionTimerElapsed;
        _fftTimer.Elapsed -= OnFftTimerElapsed;
        _positionTimer.Dispose();
        _fftTimer.Dispose();
        CleanupPlayback();

        _logger.LogInformation("AudioPlaybackService disposed.");
    }
}
