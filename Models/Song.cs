using CommunityToolkit.Mvvm.ComponentModel;

namespace NewWinampClassic.Models;

public partial class Song : ObservableObject
{
    [ObservableProperty] private int _id;
    [ObservableProperty] private string _filePath = string.Empty;
    [ObservableProperty] private string _title = string.Empty;
    [ObservableProperty] private string _artist = "Unknown Artist";
    [ObservableProperty] private string _album = "Unknown Album";
    [ObservableProperty] private string _genre = string.Empty;
    [ObservableProperty] private int _trackNumber;
    [ObservableProperty] private int _year;
    [ObservableProperty] private TimeSpan _duration;
    [ObservableProperty] private int _bitRate;
    [ObservableProperty] private string _artworkPath = string.Empty;
    [ObservableProperty] private int _artistId = -1;
    [ObservableProperty] private int _albumId = -1;
    [ObservableProperty] private int _genreId = -1;
    [ObservableProperty] private int _playCount;
    [ObservableProperty] private DateTime _dateAdded;
    [ObservableProperty] private DateTime? _lastPlayed;
    [ObservableProperty] private bool _isFavorite;
    [ObservableProperty] private bool _isNowPlaying;
    [ObservableProperty] private string _fileFormat = string.Empty;
    [ObservableProperty] private long _fileSize;

    public string DisplayTitle => string.IsNullOrWhiteSpace(Title) ? Path.GetFileNameWithoutExtension(FilePath) : Title;
    public string DisplayArtist => string.IsNullOrWhiteSpace(Artist) ? "Unknown Artist" : Artist;
    public string DisplayAlbum => string.IsNullOrWhiteSpace(Album) ? "Unknown Album" : Album;
    public string DurationText => Duration.ToString(@"mm\:ss");
    public string FileSizeText
    {
        get
        {
            if (FileSize < 1024 * 1024) return $"{FileSize / 1024.0:F1} KB";
            if (FileSize < 1024 * 1024 * 1024) return $"{FileSize / (1024.0 * 1024):F1} MB";
            return $"{FileSize / (1024.0 * 1024 * 1024):F2} GB";
        }
    }

    [CommunityToolkit.Mvvm.Input.RelayCommand]
    public async Task ToggleFavoriteAsync()
    {
        var libraryService = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<NewWinampClassic.Services.IMusicLibraryService>(App.Services);
        bool newFav = await libraryService.ToggleFavoriteAsync(Id);
        IsFavorite = newFav;
    }
}
