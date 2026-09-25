using NewWinampClassic.Models;

namespace NewWinampClassic.Services;

public interface IMusicLibraryService
{
    IProgress<ScanningProgress>? ScanProgress { get; set; }

    Task ScanLibraryAsync(string rootPath, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Song>> GetAllSongsAsync();
    Task<IReadOnlyList<Song>> SearchSongsAsync(string query);
    Task<IReadOnlyList<Song>> GetSongsByAlbumAsync(string albumName);
    Task<IReadOnlyList<Song>> GetSongsByArtistAsync(int artistId);
    Task<IReadOnlyList<Album>> GetAlbumsAsync();
    Task<IReadOnlyList<Artist>> GetArtistsAsync();
    Task<IReadOnlyList<Genre>> GetGenresAsync();
    Task<IReadOnlyList<Song>> GetRecentSongsAsync(int count = 50);
    Task<IReadOnlyList<Song>> GetMostPlayedAsync(int count = 50);
    Task<Song> AddSongAsync(string filePath);
    event EventHandler<Song>? SongFavoriteToggled;
    event EventHandler? LibraryScanCompleted;

    Task RemoveSongAsync(int songId);
    Task<bool> ToggleFavoriteAsync(int songId);
    Task<IReadOnlyList<Song>> GetFavoritesAsync();
    Task AddSongToHistoryAsync(int songId);

    int TotalSongs { get; }
}
