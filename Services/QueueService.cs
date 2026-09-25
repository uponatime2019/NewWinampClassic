using System.Collections.ObjectModel;
using NewWinampClassic.Models;

namespace NewWinampClassic.Services;

public sealed class QueueService : IQueueService
{
    private readonly Random _random = new();
    private List<int> _shuffledIndices = [];

    public ObservableCollection<Song> Queue { get; } = [];

    private int _currentIndex = -1;
    public int CurrentIndex
    {
        get => _currentIndex;
        private set
        {
            var oldIndex = _currentIndex;
            _currentIndex = value;

            if (oldIndex != _currentIndex)
            {
                CurrentSongChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    public Song? CurrentSong =>
        CurrentIndex >= 0 && CurrentIndex < Queue.Count
            ? Queue[CurrentIndex]
            : null;

    private bool _isShuffleEnabled;
    public bool IsShuffleEnabled
    {
        get => _isShuffleEnabled;
        set
        {
            _isShuffleEnabled = value;
            if (value)
            {
                RegenerateShuffleIndices();
            }
        }
    }

    public RepeatMode RepeatMode { get; set; } = RepeatMode.None;

    public event EventHandler? QueueChanged;
    public event EventHandler? CurrentSongChanged;

    public void SetQueue(IEnumerable<Song> songs, int startIndex = 0)
    {
        Queue.CollectionChanged -= OnQueueCollectionChanged;

        Queue.Clear();
        foreach (var song in songs)
        {
            Queue.Add(song);
        }

        Queue.CollectionChanged += OnQueueCollectionChanged;

        if (_isShuffleEnabled)
        {
            RegenerateShuffleIndices();
            CurrentIndex = startIndex < _shuffledIndices.Count
                ? _shuffledIndices[startIndex]
                : (Queue.Count > 0 ? 0 : -1);
        }
        else
        {
            CurrentIndex = Queue.Count > 0
                ? Math.Clamp(startIndex, 0, Queue.Count - 1)
                : -1;
        }

        QueueChanged?.Invoke(this, EventArgs.Empty);
    }

    public void AddToQueue(Song song)
    {
        Queue.Add(song);

        if (_isShuffleEnabled)
        {
            RegenerateShuffleIndices();
        }

        if (CurrentIndex < 0)
        {
            CurrentIndex = 0;
        }

        QueueChanged?.Invoke(this, EventArgs.Empty);
    }

    public void AddRangeToQueue(IEnumerable<Song> songs)
    {
        var songList = songs.ToList();
        if (songList.Count == 0)
            return;

        var wasEmpty = Queue.Count == 0;

        foreach (var song in songList)
        {
            Queue.Add(song);
        }

        if (_isShuffleEnabled)
        {
            RegenerateShuffleIndices();
        }

        if (wasEmpty)
        {
            CurrentIndex = 0;
        }

        QueueChanged?.Invoke(this, EventArgs.Empty);
    }

    public void RemoveFromQueue(int index)
    {
        if (index < 0 || index >= Queue.Count)
            return;

        Queue.RemoveAt(index);

        if (_isShuffleEnabled)
        {
            RegenerateShuffleIndices();
        }

        if (Queue.Count == 0)
        {
            CurrentIndex = -1;
        }
        else if (index < CurrentIndex)
        {
            CurrentIndex = Math.Max(0, CurrentIndex - 1);
        }
        else if (index == CurrentIndex)
        {
            if (CurrentIndex >= Queue.Count)
            {
                CurrentIndex = Queue.Count - 1;
            }
        }

        QueueChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Move(int fromIndex, int toIndex)
    {
        if (fromIndex < 0 || fromIndex >= Queue.Count)
            return;
        if (toIndex < 0 || toIndex >= Queue.Count)
            return;
        if (fromIndex == toIndex)
            return;

        var song = Queue[fromIndex];
        Queue.RemoveAt(fromIndex);
        Queue.Insert(toIndex, song);

        if (CurrentIndex == fromIndex)
        {
            CurrentIndex = toIndex;
        }
        else if (fromIndex < CurrentIndex && toIndex >= CurrentIndex)
        {
            CurrentIndex--;
        }
        else if (fromIndex > CurrentIndex && toIndex <= CurrentIndex)
        {
            CurrentIndex++;
        }

        if (_isShuffleEnabled)
        {
            RegenerateShuffleIndices();
        }

        QueueChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Clear()
    {
        Queue.Clear();
        CurrentIndex = -1;
        _shuffledIndices.Clear();

        QueueChanged?.Invoke(this, EventArgs.Empty);
    }

    public Song? Next()
    {
        if (Queue.Count == 0)
            return null;

        switch (RepeatMode)
        {
            case RepeatMode.One:
                return CurrentSong;

            case RepeatMode.All:
                if (_isShuffleEnabled)
                {
                    var nextShufflePos = GetNextShufflePosition();
                    CurrentIndex = _shuffledIndices[nextShufflePos];
                }
                else
                {
                    CurrentIndex = (CurrentIndex + 1) % Queue.Count;
                }
                return CurrentSong;

            case RepeatMode.None:
            default:
                if (_isShuffleEnabled)
                {
                    var currentShufflePos = GetCurrentShufflePosition();
                    if (currentShufflePos >= _shuffledIndices.Count - 1)
                    {
                        return null;
                    }
                    CurrentIndex = _shuffledIndices[currentShufflePos + 1];
                }
                else
                {
                    if (CurrentIndex >= Queue.Count - 1)
                    {
                        return null;
                    }
                    CurrentIndex++;
                }
                return CurrentSong;
        }
    }

    public Song? Previous()
    {
        if (Queue.Count == 0)
            return null;

        switch (RepeatMode)
        {
            case RepeatMode.One:
                return CurrentSong;

            case RepeatMode.All:
                if (_isShuffleEnabled)
                {
                    var prevShufflePos = GetPreviousShufflePosition();
                    CurrentIndex = _shuffledIndices[prevShufflePos];
                }
                else
                {
                    CurrentIndex = (CurrentIndex - 1 + Queue.Count) % Queue.Count;
                }
                return CurrentSong;

            case RepeatMode.None:
            default:
                if (_isShuffleEnabled)
                {
                    var currentShufflePos = GetCurrentShufflePosition();
                    if (currentShufflePos <= 0)
                    {
                        return null;
                    }
                    CurrentIndex = _shuffledIndices[currentShufflePos - 1];
                }
                else
                {
                    if (CurrentIndex <= 0)
                    {
                        return null;
                    }
                    CurrentIndex--;
                }
                return CurrentSong;
        }
    }

    public void PlayAt(int index)
    {
        if (index < 0 || index >= Queue.Count)
            return;

        CurrentIndex = index;
    }

    private void RegenerateShuffleIndices()
    {
        _shuffledIndices = Enumerable.Range(0, Queue.Count).ToList();

        for (var i = _shuffledIndices.Count - 1; i > 0; i--)
        {
            var j = _random.Next(i + 1);
            (_shuffledIndices[i], _shuffledIndices[j]) = (_shuffledIndices[j], _shuffledIndices[i]);
        }

        if (CurrentIndex >= 0 && _shuffledIndices.Count > 0)
        {
            var currentShufflePos = _shuffledIndices.IndexOf(CurrentIndex);
            if (currentShufflePos > 0)
            {
                (_shuffledIndices[0], _shuffledIndices[currentShufflePos]) =
                    (_shuffledIndices[currentShufflePos], _shuffledIndices[0]);
            }
        }
    }

    private int GetCurrentShufflePosition()
    {
        if (CurrentIndex >= 0)
        {
            var pos = _shuffledIndices.IndexOf(CurrentIndex);
            if (pos >= 0)
                return pos;
        }
        return 0;
    }

    private int GetNextShufflePosition()
    {
        var current = GetCurrentShufflePosition();
        return (current + 1) % _shuffledIndices.Count;
    }

    private int GetPreviousShufflePosition()
    {
        var current = GetCurrentShufflePosition();
        return (current - 1 + _shuffledIndices.Count) % _shuffledIndices.Count;
    }

    private void OnQueueCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        // Collection changes are handled in the public methods above
    }
}
