using CommunityToolkit.Mvvm.ComponentModel;

namespace NewWinampClassic.Models;

public partial class PlaylistSong : ObservableObject
{
    [ObservableProperty] private int _id;
    [ObservableProperty] private int _playlistId;
    [ObservableProperty] private int _songId;
    [ObservableProperty] private int _position;
    [ObservableProperty] private DateTime _dateAdded;
    [ObservableProperty] private Song? _song;
}
