using CommunityToolkit.Mvvm.ComponentModel;

namespace NewWinampClassic.Models;

public partial class Artist : ObservableObject
{
    [ObservableProperty] private int _id;
    [ObservableProperty] private string _name = "Unknown Artist";
    [ObservableProperty] private int _albumCount;
    [ObservableProperty] private int _trackCount;
    [ObservableProperty] private string _artworkPath = string.Empty;

    public string TrackCountText => $"{TrackCount} songs";
    public string AlbumCountText => $"{AlbumCount} albums";
}
