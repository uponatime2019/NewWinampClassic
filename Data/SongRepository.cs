using Microsoft.Data.Sqlite;
using NewWinampClassic.Models;

namespace NewWinampClassic.Data;

/// <summary>
/// SQLite-backed implementation of <see cref="ISongRepository"/>.
/// The connection is expected to be already open and managed externally.
/// </summary>
public sealed class SongRepository : ISongRepository
{
    private readonly SqliteConnection _connection;

    public SongRepository(SqliteConnection connection)
    {
        _connection = connection;
    }

    // ---------------------------------------------------------------------
    // Read operations
    // ---------------------------------------------------------------------

    public async Task<Song?> GetByIdAsync(int id)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            SELECT Id, FilePath, Title, Artist, Album, Genre,
                   TrackNumber, Year, Duration, BitRate, ArtworkPath,
                   ArtistId, AlbumId, GenreId, PlayCount, DateAdded,
                   LastPlayed, IsFavorite, FileFormat, FileSize
            FROM Songs
            WHERE Id = @Id
            """;
        cmd.Parameters.AddWithValue("@Id", id);

        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
            return MapSong(reader);

        return null;
    }

    public async Task<Song?> GetByFilePathAsync(string filePath)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            SELECT Id, FilePath, Title, Artist, Album, Genre,
                   TrackNumber, Year, Duration, BitRate, ArtworkPath,
                   ArtistId, AlbumId, GenreId, PlayCount, DateAdded,
                   LastPlayed, IsFavorite, FileFormat, FileSize
            FROM Songs
            WHERE FilePath = @FilePath
            """;
        cmd.Parameters.AddWithValue("@FilePath", filePath);

        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
            return MapSong(reader);

        return null;
    }

    public async Task<IReadOnlyList<Song>> GetAllAsync()
    {
        return await QuerySongsAsync("""
            SELECT Id, FilePath, Title, Artist, Album, Genre,
                   TrackNumber, Year, Duration, BitRate, ArtworkPath,
                   ArtistId, AlbumId, GenreId, PlayCount, DateAdded,
                   LastPlayed, IsFavorite, FileFormat, FileSize
            FROM Songs
            ORDER BY Title
            """);
    }

    public async Task<IReadOnlyList<Song>> GetPagedAsync(int offset, int limit, string sortBy = "Title", bool descending = false)
    {
        // Whitelist allowed sort columns to prevent SQL injection.
        var column = sortBy switch
        {
            "Title" => "Title",
            "Artist" => "Artist",
            "Album" => "Album",
            "Genre" => "Genre",
            "Year" => "Year",
            "Duration" => "Duration",
            "DateAdded" => "DateAdded",
            "PlayCount" => "PlayCount",
            _ => "Title"
        };
        var direction = descending ? "DESC" : "ASC";

        var sql = $"""
            SELECT Id, FilePath, Title, Artist, Album, Genre,
                   TrackNumber, Year, Duration, BitRate, ArtworkPath,
                   ArtistId, AlbumId, GenreId, PlayCount, DateAdded,
                   LastPlayed, IsFavorite, FileFormat, FileSize
            FROM Songs
            ORDER BY {column} {direction}
            LIMIT @Limit OFFSET @Offset
            """;

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue("@Limit", limit);
        cmd.Parameters.AddWithValue("@Offset", offset);

        return await ReadSongListAsync(cmd);
    }

    public async Task<IReadOnlyList<Song>> SearchAsync(string query, int limit = 50)
    {
        const string sql = """
            SELECT Id, FilePath, Title, Artist, Album, Genre,
                   TrackNumber, Year, Duration, BitRate, ArtworkPath,
                   ArtistId, AlbumId, GenreId, PlayCount, DateAdded,
                   LastPlayed, IsFavorite, FileFormat, FileSize
            FROM Songs
            WHERE Title LIKE @Query
               OR Artist LIKE @Query
               OR Album LIKE @Query
            ORDER BY Title
            LIMIT @Limit
            """;

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue("@Query", $"%{query}%");
        cmd.Parameters.AddWithValue("@Limit", limit);

        return await ReadSongListAsync(cmd);
    }

    public async Task<IReadOnlyList<Song>> GetByAlbumAsync(int albumId)
    {
        return await QuerySongsAsync("""
            SELECT Id, FilePath, Title, Artist, Album, Genre,
                   TrackNumber, Year, Duration, BitRate, ArtworkPath,
                   ArtistId, AlbumId, GenreId, PlayCount, DateAdded,
                   LastPlayed, IsFavorite, FileFormat, FileSize
            FROM Songs
            WHERE AlbumId = @AlbumId
            ORDER BY TrackNumber, Title
            """,
            ("@AlbumId", albumId));
    }

    public async Task<IReadOnlyList<Song>> GetByAlbumNameAsync(string albumName)
    {
        return await QuerySongsAsync("""
            SELECT Id, FilePath, Title, Artist, Album, Genre,
                   TrackNumber, Year, Duration, BitRate, ArtworkPath,
                   ArtistId, AlbumId, GenreId, PlayCount, DateAdded,
                   LastPlayed, IsFavorite, FileFormat, FileSize
            FROM Songs
            WHERE Album = @AlbumName
            ORDER BY TrackNumber, Title
            """,
            ("@AlbumName", albumName));
    }

    public async Task<IReadOnlyList<Song>> GetByArtistAsync(int artistId)
    {
        return await QuerySongsAsync("""
            SELECT Id, FilePath, Title, Artist, Album, Genre,
                   TrackNumber, Year, Duration, BitRate, ArtworkPath,
                   ArtistId, AlbumId, GenreId, PlayCount, DateAdded,
                   LastPlayed, IsFavorite, FileFormat, FileSize
            FROM Songs
            WHERE ArtistId = @ArtistId
            ORDER BY Title
            """,
            ("@ArtistId", artistId));
    }

    public async Task<IReadOnlyList<Song>> GetFavoritesAsync()
    {
        return await QuerySongsAsync("""
            SELECT s.Id, s.FilePath, s.Title, s.Artist, s.Album, s.Genre,
                   s.TrackNumber, s.Year, s.Duration, s.BitRate, s.ArtworkPath,
                   s.ArtistId, s.AlbumId, s.GenreId, s.PlayCount, s.DateAdded,
                   s.LastPlayed, s.IsFavorite, s.FileFormat, s.FileSize
            FROM Songs s
            INNER JOIN Favorites f ON s.Id = f.SongId
            ORDER BY f.AddedAt DESC
            """);
    }

    public async Task<IReadOnlyList<Song>> GetRecentlyPlayedAsync(int count = 50)
    {
        const string sql = """
            SELECT s.Id, s.FilePath, s.Title, s.Artist, s.Album, s.Genre,
                   s.TrackNumber, s.Year, s.Duration, s.BitRate, s.ArtworkPath,
                   s.ArtistId, s.AlbumId, s.GenreId, s.PlayCount, s.DateAdded,
                   s.LastPlayed, s.IsFavorite, s.FileFormat, s.FileSize
            FROM Songs s
            INNER JOIN (
                SELECT SongId, MAX(PlayedAt) AS LastPlay
                FROM History
                GROUP BY SongId
            ) h ON s.Id = h.SongId
            ORDER BY h.LastPlay DESC
            LIMIT @Count
            """;

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue("@Count", count);

        return await ReadSongListAsync(cmd);
    }

    public async Task<IReadOnlyList<Song>> GetMostPlayedAsync(int count = 50)
    {
        const string sql = """
            SELECT Id, FilePath, Title, Artist, Album, Genre,
                   TrackNumber, Year, Duration, BitRate, ArtworkPath,
                   ArtistId, AlbumId, GenreId, PlayCount, DateAdded,
                   LastPlayed, IsFavorite, FileFormat, FileSize
            FROM Songs
            WHERE PlayCount > 0
            ORDER BY PlayCount DESC
            LIMIT @Count
            """;

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue("@Count", count);

        return await ReadSongListAsync(cmd);
    }

    public async Task<IReadOnlyList<Song>> GetByGenreAsync(int genreId)
    {
        return await QuerySongsAsync("""
            SELECT Id, FilePath, Title, Artist, Album, Genre,
                   TrackNumber, Year, Duration, BitRate, ArtworkPath,
                   ArtistId, AlbumId, GenreId, PlayCount, DateAdded,
                   LastPlayed, IsFavorite, FileFormat, FileSize
            FROM Songs
            WHERE GenreId = @GenreId
            ORDER BY Title
            """,
            ("@GenreId", genreId));
    }

    public async Task<int> GetCountAsync()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM Songs";
        var result = await cmd.ExecuteScalarAsync();
        return result is long l ? (int)l : 0;
    }

    // ---------------------------------------------------------------------
    // Write operations
    // ---------------------------------------------------------------------

    public async Task<Song> AddOrUpdateAsync(Song song)
    {
        // Check if a song with the same FilePath already exists.
        var existing = await GetByFilePathAsync(song.FilePath);

        if (existing is not null)
        {
            // Preserve the existing Id and update all other columns.
            song.Id = existing.Id;
            await UpdateAsync(song);
            return song;
        }

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            INSERT INTO Songs (FilePath, Title, Artist, Album, Genre,
                TrackNumber, Year, Duration, BitRate, ArtworkPath,
                ArtistId, AlbumId, GenreId, PlayCount, DateAdded,
                LastPlayed, IsFavorite, FileFormat, FileSize)
            VALUES (@FilePath, @Title, @Artist, @Album, @Genre,
                @TrackNumber, @Year, @Duration, @BitRate, @ArtworkPath,
                @ArtistId, @AlbumId, @GenreId, @PlayCount, @DateAdded,
                @LastPlayed, @IsFavorite, @FileFormat, @FileSize);
            SELECT last_insert_rowid();
            """;

        AddSongParameters(cmd, song);

        var result = await cmd.ExecuteScalarAsync();
        song.Id = result is long l ? (int)l : song.Id;
        return song;
    }

    public async Task UpdateAsync(Song song)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            UPDATE Songs SET
                FilePath = @FilePath, Title = @Title, Artist = @Artist,
                Album = @Album, Genre = @Genre, TrackNumber = @TrackNumber,
                Year = @Year, Duration = @Duration, BitRate = @BitRate,
                ArtworkPath = @ArtworkPath, ArtistId = @ArtistId,
                AlbumId = @AlbumId, GenreId = @GenreId, PlayCount = @PlayCount,
                DateAdded = @DateAdded, LastPlayed = @LastPlayed,
                IsFavorite = @IsFavorite, FileFormat = @FileFormat,
                FileSize = @FileSize
            WHERE Id = @Id
            """;

        cmd.Parameters.AddWithValue("@Id", song.Id);
        AddSongParameters(cmd, song);

        await cmd.ExecuteNonQueryAsync();
    }

    public async Task DeleteAsync(int id)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "DELETE FROM Songs WHERE Id = @Id";
        cmd.Parameters.AddWithValue("@Id", id);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<bool> ToggleFavoriteAsync(int songId)
    {
        using var cmd = _connection.CreateCommand();

        // First check current favorite status.
        cmd.CommandText = "SELECT IsFavorite FROM Songs WHERE Id = @Id";
        cmd.Parameters.AddWithValue("@Id", songId);
        var current = await cmd.ExecuteScalarAsync();
        bool isFavorite = current is long l && l == 1;

        // Toggle the IsFavorite column on the song.
        cmd.Parameters.Clear();
        cmd.CommandText = "UPDATE Songs SET IsFavorite = @Value WHERE Id = @Id";
        cmd.Parameters.AddWithValue("@Value", isFavorite ? 0 : 1);
        cmd.Parameters.AddWithValue("@Id", songId);
        await cmd.ExecuteNonQueryAsync();

        // Maintain the Favorites table.
        cmd.Parameters.Clear();
        if (isFavorite)
        {
            cmd.CommandText = "DELETE FROM Favorites WHERE SongId = @SongId";
            cmd.Parameters.AddWithValue("@SongId", songId);
        }
        else
        {
            cmd.CommandText = """
                INSERT OR REPLACE INTO Favorites (SongId, AddedAt)
                VALUES (@SongId, @AddedAt)
                """;
            cmd.Parameters.AddWithValue("@SongId", songId);
            cmd.Parameters.AddWithValue("@AddedAt", DateTime.UtcNow.ToString("O"));
        }
        await cmd.ExecuteNonQueryAsync();
        return !isFavorite;
    }

    public async Task IncrementPlayCountAsync(int songId)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            UPDATE Songs
            SET PlayCount = PlayCount + 1,
                LastPlayed = @LastPlayed
            WHERE Id = @Id
            """;
        cmd.Parameters.AddWithValue("@Id", songId);
        cmd.Parameters.AddWithValue("@LastPlayed", DateTime.UtcNow.ToString("O"));
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task AddToHistoryAsync(int songId, double position = 0)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            INSERT INTO History (SongId, PlayedAt, PlaybackPosition)
            VALUES (@SongId, @PlayedAt, @PlaybackPosition)
            """;
        cmd.Parameters.AddWithValue("@SongId", songId);
        cmd.Parameters.AddWithValue("@PlayedAt", DateTime.UtcNow.ToString("O"));
        cmd.Parameters.AddWithValue("@PlaybackPosition", position);
        await cmd.ExecuteNonQueryAsync();
    }

    // ---------------------------------------------------------------------
    // Mapping helpers
    // ---------------------------------------------------------------------

    private static Song MapSong(SqliteDataReader reader)
    {
        return new Song
        {
            Id = reader.GetInt32(reader.GetOrdinal("Id")),
            FilePath = reader.GetString(reader.GetOrdinal("FilePath")),
            Title = reader.IsDBNull(reader.GetOrdinal("Title"))
                ? string.Empty
                : reader.GetString(reader.GetOrdinal("Title")),
            Artist = reader.IsDBNull(reader.GetOrdinal("Artist"))
                ? "Unknown Artist"
                : reader.GetString(reader.GetOrdinal("Artist")),
            Album = reader.IsDBNull(reader.GetOrdinal("Album"))
                ? "Unknown Album"
                : reader.GetString(reader.GetOrdinal("Album")),
            Genre = reader.IsDBNull(reader.GetOrdinal("Genre"))
                ? string.Empty
                : reader.GetString(reader.GetOrdinal("Genre")),
            TrackNumber = reader.IsDBNull(reader.GetOrdinal("TrackNumber"))
                ? 0
                : reader.GetInt32(reader.GetOrdinal("TrackNumber")),
            Year = reader.IsDBNull(reader.GetOrdinal("Year"))
                ? 0
                : reader.GetInt32(reader.GetOrdinal("Year")),
            Duration = TimeSpan.FromTicks(reader.GetInt64(reader.GetOrdinal("Duration"))),
            BitRate = reader.IsDBNull(reader.GetOrdinal("BitRate"))
                ? 0
                : reader.GetInt32(reader.GetOrdinal("BitRate")),
            ArtworkPath = reader.IsDBNull(reader.GetOrdinal("ArtworkPath"))
                ? string.Empty
                : reader.GetString(reader.GetOrdinal("ArtworkPath")),
            ArtistId = reader.IsDBNull(reader.GetOrdinal("ArtistId"))
                ? -1
                : reader.GetInt32(reader.GetOrdinal("ArtistId")),
            AlbumId = reader.IsDBNull(reader.GetOrdinal("AlbumId"))
                ? -1
                : reader.GetInt32(reader.GetOrdinal("AlbumId")),
            GenreId = reader.IsDBNull(reader.GetOrdinal("GenreId"))
                ? -1
                : reader.GetInt32(reader.GetOrdinal("GenreId")),
            PlayCount = reader.IsDBNull(reader.GetOrdinal("PlayCount"))
                ? 0
                : reader.GetInt32(reader.GetOrdinal("PlayCount")),
            DateAdded = DateTime.Parse(
                reader.GetString(reader.GetOrdinal("DateAdded")),
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.RoundtripKind),
            LastPlayed = reader.IsDBNull(reader.GetOrdinal("LastPlayed"))
                ? null
                : DateTime.Parse(
                    reader.GetString(reader.GetOrdinal("LastPlayed")),
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.RoundtripKind),
            IsFavorite = !reader.IsDBNull(reader.GetOrdinal("IsFavorite"))
                && reader.GetInt32(reader.GetOrdinal("IsFavorite")) == 1,
            FileFormat = reader.IsDBNull(reader.GetOrdinal("FileFormat"))
                ? string.Empty
                : reader.GetString(reader.GetOrdinal("FileFormat")),
            FileSize = reader.IsDBNull(reader.GetOrdinal("FileSize"))
                ? 0
                : reader.GetInt64(reader.GetOrdinal("FileSize")),
        };
    }

    private static void AddSongParameters(SqliteCommand cmd, Song song)
    {
        cmd.Parameters.AddWithValue("@FilePath", song.FilePath);
        cmd.Parameters.AddWithValue("@Title", song.Title as object ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Artist", song.Artist);
        cmd.Parameters.AddWithValue("@Album", song.Album);
        cmd.Parameters.AddWithValue("@Genre", song.Genre as object ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@TrackNumber", song.TrackNumber);
        cmd.Parameters.AddWithValue("@Year", song.Year);
        cmd.Parameters.AddWithValue("@Duration", song.Duration.Ticks);
        cmd.Parameters.AddWithValue("@BitRate", song.BitRate);
        cmd.Parameters.AddWithValue("@ArtworkPath", song.ArtworkPath as object ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@ArtistId", song.ArtistId);
        cmd.Parameters.AddWithValue("@AlbumId", song.AlbumId);
        cmd.Parameters.AddWithValue("@GenreId", song.GenreId);
        cmd.Parameters.AddWithValue("@PlayCount", song.PlayCount);
        cmd.Parameters.AddWithValue("@DateAdded", song.DateAdded.ToString("O"));
        cmd.Parameters.AddWithValue("@LastPlayed",
            song.LastPlayed.HasValue ? song.LastPlayed.Value.ToString("O") : DBNull.Value);
        cmd.Parameters.AddWithValue("@IsFavorite", song.IsFavorite ? 1 : 0);
        cmd.Parameters.AddWithValue("@FileFormat", song.FileFormat as object ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@FileSize", song.FileSize);
    }

    // ---------------------------------------------------------------------
    // Query helpers
    // ---------------------------------------------------------------------

    /// <summary>
    /// Executes a SELECT query that returns Song rows. Optionally accepts a
    /// single typed parameter as a tuple of (name, value).
    /// </summary>
    private async Task<IReadOnlyList<Song>> QuerySongsAsync(string sql, (string Name, object Value)? param = null)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = sql;

        if (param is not null)
            cmd.Parameters.AddWithValue(param.Value.Name, param.Value.Value);

        return await ReadSongListAsync(cmd);
    }

    private static async Task<IReadOnlyList<Song>> ReadSongListAsync(SqliteCommand cmd)
    {
        var songs = new List<Song>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            songs.Add(MapSong(reader));
        }
        return songs;
    }
}
