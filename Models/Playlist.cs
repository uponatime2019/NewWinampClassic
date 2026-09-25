using CommunityToolkit.Mvvm.ComponentModel;

namespace NewWinampClassic.Models;

public partial class Playlist : ObservableObject
{
    [ObservableProperty] private int _id;
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string _description = string.Empty;
    [ObservableProperty] private string _artworkPath = string.Empty;
    [ObservableProperty] private int _trackCount;
    [ObservableProperty] private TimeSpan _totalDuration;
    [ObservableProperty] private DateTime _createdAt;
    [ObservableProperty] private DateTime _updatedAt;
    [ObservableProperty] private bool _isSmartPlaylist;
    [ObservableProperty] private string _smartQuery = string.Empty;

    /// <summary>
    /// Display-friendly name with emoji prefix for smart playlists.
    /// </summary>
    public string DisplayName => SmartQuery switch
    {
        "favorites" => "❤️ Favorites",
        "recent" => "⏰ Recent",
        _ => Name
    };
}
