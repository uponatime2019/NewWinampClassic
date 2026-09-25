using System.Collections.ObjectModel;
using NewWinampClassic.Models;

namespace NewWinampClassic.Services;

public interface IQueueService
{
    ObservableCollection<Song> Queue { get; }
    Song? CurrentSong { get; }
    int CurrentIndex { get; }
    bool IsShuffleEnabled { get; set; }
    RepeatMode RepeatMode { get; set; }

    event EventHandler? QueueChanged;
    event EventHandler? CurrentSongChanged;

    void SetQueue(IEnumerable<Song> songs, int startIndex = 0);
    void AddToQueue(Song song);
    void AddRangeToQueue(IEnumerable<Song> songs);
    void RemoveFromQueue(int index);
    void Move(int fromIndex, int toIndex);
    void Clear();
    Song? Next();
    Song? Previous();
    void PlayAt(int index);
}
