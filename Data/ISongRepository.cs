using NewWinampClassic.Models;

namespace NewWinampClassic.Data;

/// <summary>
/// Defines the contract for a repository that manages <see cref="Song"/>
/// persistence and related queries.
/// </summary>
public interface ISongRepository
{
    Task<Song?> GetByIdAsync(int id);
    Task<Song?> GetByFilePathAsync(string filePath);
    Task<IReadOnlyList<Song>> GetAllAsync();
    Task<IReadOnlyList<Song>> GetPagedAsync(int offset, int limit, string sortBy = "Title", bool descending = false);
    Task<IReadOnlyList<Song>> SearchAsync(string query, int limit = 50);
    Task<IReadOnlyList<Song>> GetByAlbumAsync(int albumId);
    Task<IReadOnlyList<Song>> GetByAlbumNameAsync(string albumName);
    Task<IReadOnlyList<Song>> GetByArtistAsync(int artistId);
    Task<IReadOnlyList<Song>> GetFavoritesAsync();
    Task<IReadOnlyList<Song>> GetRecentlyPlayedAsync(int count = 50);
    Task<IReadOnlyList<Song>> GetMostPlayedAsync(int count = 50);
    Task<IReadOnlyList<Song>> GetByGenreAsync(int genreId);
    Task<Song> AddOrUpdateAsync(Song song);
    Task UpdateAsync(Song song);
    Task DeleteAsync(int id);
    Task<int> GetCountAsync();
    Task<bool> ToggleFavoriteAsync(int songId);
    Task IncrementPlayCountAsync(int songId);
    Task AddToHistoryAsync(int songId, double position = 0);
}
