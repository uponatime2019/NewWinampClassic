using CommunityToolkit.Mvvm.ComponentModel;

namespace NewWinampClassic.Models;

public partial class HistoryEntry : ObservableObject
{
    [ObservableProperty] private int _id;
    [ObservableProperty] private int _songId;
    [ObservableProperty] private DateTime _playedAt;
    [ObservableProperty] private double _playbackPosition;
    [ObservableProperty] private Song? _song;
}
