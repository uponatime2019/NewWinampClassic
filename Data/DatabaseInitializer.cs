using Microsoft.Data.Sqlite;

namespace NewWinampClassic.Data;

/// <summary>
/// Creates all SQLite tables and indexes if they do not already exist.
/// </summary>
public static class DatabaseInitializer
{
    /// <summary>
    /// Executes all CREATE TABLE and CREATE INDEX statements against the
    /// supplied connection. The connection must already be open.
    /// </summary>
    public static async Task InitializeAsync(SqliteConnection connection)
    {
        using var transaction = connection.BeginTransaction();

        try
        {
            await CreateTablesAsync(connection);
            await CreateIndexesAsync(connection);
            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    private static async Task CreateTablesAsync(SqliteConnection connection)
    {
        await ExecuteNonQueryAsync(connection, """
            CREATE TABLE IF NOT EXISTS Songs (
                Id            INTEGER PRIMARY KEY AUTOINCREMENT,
                FilePath      TEXT    UNIQUE NOT NULL,
                Title         TEXT,
                Artist        TEXT    DEFAULT 'Unknown Artist',
                Album         TEXT    DEFAULT 'Unknown Album',
                Genre         TEXT,
                TrackNumber   INTEGER DEFAULT 0,
                Year          INTEGER DEFAULT 0,
                Duration      INTEGER NOT NULL,
                BitRate       INTEGER DEFAULT 0,
                ArtworkPath   TEXT,
                ArtistId      INTEGER DEFAULT -1,
                AlbumId       INTEGER DEFAULT -1,
                GenreId       INTEGER DEFAULT -1,
                PlayCount     INTEGER DEFAULT 0,
                DateAdded     TEXT    NOT NULL,
                LastPlayed    TEXT,
                IsFavorite    INTEGER DEFAULT 0,
                FileFormat    TEXT,
                FileSize      INTEGER DEFAULT 0
            )
            """);

        await ExecuteNonQueryAsync(connection, """
            CREATE TABLE IF NOT EXISTS Albums (
                Id            INTEGER PRIMARY KEY AUTOINCREMENT,
                Name          TEXT    NOT NULL,
                Artist        TEXT    DEFAULT 'Unknown Artist',
                ArtistId      INTEGER DEFAULT -1,
                ArtworkPath   TEXT,
                Year          INTEGER DEFAULT 0,
                TrackCount    INTEGER DEFAULT 0,
                TotalDuration INTEGER DEFAULT 0,
                DateAdded     TEXT    NOT NULL
            )
            """);

        await ExecuteNonQueryAsync(connection, """
            CREATE TABLE IF NOT EXISTS Artists (
                Id          INTEGER PRIMARY KEY AUTOINCREMENT,
                Name        TEXT UNIQUE NOT NULL,
                AlbumCount  INTEGER DEFAULT 0,
                TrackCount  INTEGER DEFAULT 0,
                ArtworkPath TEXT
            )
            """);

        await ExecuteNonQueryAsync(connection, """
            CREATE TABLE IF NOT EXISTS Genres (
                Id        INTEGER PRIMARY KEY AUTOINCREMENT,
                Name      TEXT UNIQUE NOT NULL,
                TrackCount INTEGER DEFAULT 0
            )
            """);

        await ExecuteNonQueryAsync(connection, """
            CREATE TABLE IF NOT EXISTS Playlists (
                Id              INTEGER PRIMARY KEY AUTOINCREMENT,
                Name            TEXT NOT NULL,
                Description     TEXT DEFAULT '',
                ArtworkPath     TEXT,
                TrackCount      INTEGER DEFAULT 0,
                TotalDuration   INTEGER DEFAULT 0,
                CreatedAt       TEXT NOT NULL,
                UpdatedAt       TEXT NOT NULL,
                IsSmartPlaylist INTEGER DEFAULT 0,
                SmartQuery      TEXT DEFAULT ''
            )
            """);

        await ExecuteNonQueryAsync(connection, """
            CREATE TABLE IF NOT EXISTS PlaylistSongs (
                Id        INTEGER PRIMARY KEY AUTOINCREMENT,
                PlaylistId INTEGER NOT NULL,
                SongId     INTEGER NOT NULL,
                Position   INTEGER NOT NULL,
                DateAdded  TEXT    NOT NULL,
                FOREIGN KEY (PlaylistId) REFERENCES Playlists(Id) ON DELETE CASCADE,
                FOREIGN KEY (SongId)     REFERENCES Songs(Id)     ON DELETE CASCADE
            )
            """);

        await ExecuteNonQueryAsync(connection, """
            CREATE TABLE IF NOT EXISTS History (
                Id              INTEGER PRIMARY KEY AUTOINCREMENT,
                SongId          INTEGER NOT NULL,
                PlayedAt        TEXT    NOT NULL,
                PlaybackPosition REAL   DEFAULT 0,
                FOREIGN KEY (SongId) REFERENCES Songs(Id)
            )
            """);

        await ExecuteNonQueryAsync(connection, """
            CREATE TABLE IF NOT EXISTS Favorites (
                SongId  INTEGER PRIMARY KEY,
                AddedAt TEXT NOT NULL,
                FOREIGN KEY (SongId) REFERENCES Songs(Id)
            )
            """);

        await ExecuteNonQueryAsync(connection, """
            CREATE TABLE IF NOT EXISTS Settings (
                Key   TEXT PRIMARY KEY,
                Value TEXT NOT NULL
            )
            """);

        await ExecuteNonQueryAsync(connection, """
            CREATE TABLE IF NOT EXISTS CachedArtwork (
                Id         INTEGER PRIMARY KEY AUTOINCREMENT,
                SourcePath TEXT NOT NULL,
                CachePath  TEXT NOT NULL,
                Hash       TEXT NOT NULL,
                CreatedAt  TEXT NOT NULL
            )
            """);
    }

    private static async Task CreateIndexesAsync(SqliteConnection connection)
    {
        var indexes = new[]
        {
            "CREATE INDEX IF NOT EXISTS idx_songs_title    ON Songs(Title)",
            "CREATE INDEX IF NOT EXISTS idx_songs_artist   ON Songs(Artist)",
            "CREATE INDEX IF NOT EXISTS idx_songs_album    ON Songs(Album)",
            "CREATE INDEX IF NOT EXISTS idx_songs_genre    ON Songs(Genre)",
            "CREATE INDEX IF NOT EXISTS idx_songs_artistid ON Songs(ArtistId)",
            "CREATE INDEX IF NOT EXISTS idx_songs_albumid  ON Songs(AlbumId)",
            "CREATE INDEX IF NOT EXISTS idx_playlistsongs_playlistid ON PlaylistSongs(PlaylistId)",
            "CREATE INDEX IF NOT EXISTS idx_history_songid  ON History(SongId)",
            "CREATE INDEX IF NOT EXISTS idx_history_playedat ON History(PlayedAt)",
        };

        foreach (var sql in indexes)
        {
            await ExecuteNonQueryAsync(connection, sql);
        }
    }

    private static async Task ExecuteNonQueryAsync(SqliteConnection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync();
    }
}
