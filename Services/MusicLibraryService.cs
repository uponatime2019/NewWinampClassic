using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using NewWinampClassic.Data;
using NewWinampClassic.Helpers;
using NewWinampClassic.Models;
using TagLib;

namespace NewWinampClassic.Services;

public sealed class MusicLibraryService : IMusicLibraryService
{
    private readonly ISongRepository _songRepository;
    private readonly ILogger<MusicLibraryService> _logger;
    private readonly SqliteConnection _connection;

    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp3", ".flac", ".wav", ".ogg", ".m4a", ".aac"
    };

    public IProgress<ScanningProgress>? ScanProgress { get; set; }
    public event EventHandler<Song>? SongFavoriteToggled;
    public event EventHandler? LibraryScanCompleted;

    public int TotalSongs => _songRepository.GetCountAsync().GetAwaiter().GetResult();

    public MusicLibraryService(
        ISongRepository songRepository,
        ILogger<MusicLibraryService> logger,
        SqliteConnection connection)
    {
        _songRepository = songRepository;
        _logger = logger;
        _connection = connection;
    }

    public async Task ScanLibraryAsync(string rootPath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(rootPath))
        {
            _logger.LogWarning("No library paths specified to scan.");
            return;
        }

        var rawFolders = rootPath.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();

        var folders = FolderUtility.NormalizeFolders(rawFolders);

        if (folders.Count == 0)
        {
            _logger.LogWarning("No valid library folders found to scan.");
            return;
        }

        _logger.LogInformation("Starting library scan at folders: {Folders}", string.Join(", ", folders));

        var files = new List<string>();
        foreach (var folder in folders)
        {
            if (!Directory.Exists(folder))
            {
                _logger.LogWarning("Scan folder path does not exist: {Folder}", folder);
                continue;
            }

            try
            {
                var folderFiles = Directory.EnumerateFiles(folder, "*.*", SearchOption.AllDirectories)
                    .Where(f => SupportedExtensions.Contains(Path.GetExtension(f)))
                    .ToList();
                files.AddRange(folderFiles);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to enumerate files in folder: {Folder}", folder);
            }
        }

        var totalFiles = files.Count;
        _logger.LogInformation("Found {TotalFiles} supported audio files.", totalFiles);

        ScanProgress?.Report(new ScanningProgress(totalFiles, 0, string.Empty, false));

        var processedFiles = 0;
        var lockObj = new object();

        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = Environment.ProcessorCount,
            CancellationToken = cancellationToken
        };

        try
        {
            Parallel.ForEach(files, parallelOptions, file =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    var song = CreateSongFromFile(file);
                    if (song is not null)
                    {
                        lock (lockObj)
                        {
                            // Preserve user data on rescan.
                            var existing = _songRepository.GetByFilePathAsync(song.FilePath)
                                .GetAwaiter().GetResult();

                            if (existing is not null)
                            {
                                song.Id = existing.Id;
                                song.PlayCount = existing.PlayCount;
                                song.IsFavorite = existing.IsFavorite;
                                song.LastPlayed = existing.LastPlayed;
                                song.DateAdded = existing.DateAdded;
                            }

                            _songRepository.AddOrUpdateAsync(song).GetAwaiter().GetResult();
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to process file: {FilePath}", file);
                }

                lock (lockObj)
                {
                    processedFiles++;
                    if (processedFiles % 10 == 0 || processedFiles == totalFiles)
                    {
                        ScanProgress?.Report(new ScanningProgress(
                            totalFiles,
                            processedFiles,
                            file,
                            false));
                    }
                }
            });
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Library scan was cancelled.");
            ScanProgress?.Report(new ScanningProgress(totalFiles, processedFiles, string.Empty, false));
            return;
        }

        await RebuildAggregatedTablesAsync();

        ScanProgress?.Report(new ScanningProgress(totalFiles, totalFiles, string.Empty, true));
        _logger.LogInformation("Library scan complete. Processed {ProcessedFiles} files.", processedFiles);
        LibraryScanCompleted?.Invoke(this, EventArgs.Empty);
    }

    public async Task<IReadOnlyList<Song>> GetAllSongsAsync()
    {
        return await _songRepository.GetAllAsync();
    }

    public async Task<IReadOnlyList<Song>> SearchSongsAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return await GetAllSongsAsync();

        return await _songRepository.SearchAsync(query);
    }

    public async Task<IReadOnlyList<Song>> GetSongsByAlbumAsync(string albumName)
    {
        return await _songRepository.GetByAlbumNameAsync(albumName);
    }

    public async Task<IReadOnlyList<Song>> GetSongsByArtistAsync(int artistId)
    {
        return await _songRepository.GetByArtistAsync(artistId);
    }

    public async Task<IReadOnlyList<Album>> GetAlbumsAsync()
    {
        var albums = new List<Album>();

        using var command = _connection.CreateCommand();
        command.CommandText = """
            SELECT
                MIN(Id) as Id,
                Album as Name,
                Artist,
                MIN(ArtistId) as ArtistId,
                MAX(ArtworkPath) as ArtworkPath,
                MAX(Year) as Year,
                COUNT(*) as TrackCount,
                SUM(Duration) as TotalDuration,
                MIN(DateAdded) as DateAdded
            FROM Songs
            WHERE Album IS NOT NULL AND Album != ''
            GROUP BY Album
            ORDER BY Album
            """;

        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            albums.Add(new Album
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                Name = reader.IsDBNull(reader.GetOrdinal("Name"))
                    ? "Unknown Album"
                    : reader.GetString(reader.GetOrdinal("Name")),
                Artist = reader.IsDBNull(reader.GetOrdinal("Artist"))
                    ? "Unknown Artist"
                    : reader.GetString(reader.GetOrdinal("Artist")),
                ArtistId = reader.IsDBNull(reader.GetOrdinal("ArtistId"))
                    ? -1
                    : reader.GetInt32(reader.GetOrdinal("ArtistId")),
                ArtworkPath = reader.IsDBNull(reader.GetOrdinal("ArtworkPath"))
                    ? string.Empty
                    : reader.GetString(reader.GetOrdinal("ArtworkPath")),
                Year = reader.IsDBNull(reader.GetOrdinal("Year"))
                    ? 0
                    : reader.GetInt32(reader.GetOrdinal("Year")),
                TrackCount = reader.GetInt32(reader.GetOrdinal("TrackCount")),
                TotalDuration = TimeSpan.FromTicks(reader.GetInt64(reader.GetOrdinal("TotalDuration"))),
                DateAdded = reader.IsDBNull(reader.GetOrdinal("DateAdded"))
                    ? DateTime.MinValue
                    : DateTime.Parse(reader.GetString(reader.GetOrdinal("DateAdded")),
                        System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.RoundtripKind)
            });
        }

        return albums;
    }

    public async Task<IReadOnlyList<Artist>> GetArtistsAsync()
    {
        var artists = new List<Artist>();

        using var command = _connection.CreateCommand();
        command.CommandText = """
            SELECT
                MIN(Id) as Id,
                Artist as Name,
                COUNT(DISTINCT Album) as AlbumCount,
                COUNT(*) as TrackCount,
                MIN(ArtworkPath) as ArtworkPath
            FROM Songs
            WHERE Artist IS NOT NULL AND Artist != ''
            GROUP BY Artist
            ORDER BY Artist
            """;

        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            artists.Add(new Artist
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                Name = reader.IsDBNull(reader.GetOrdinal("Name"))
                    ? "Unknown Artist"
                    : reader.GetString(reader.GetOrdinal("Name")),
                AlbumCount = reader.GetInt32(reader.GetOrdinal("AlbumCount")),
                TrackCount = reader.GetInt32(reader.GetOrdinal("TrackCount")),
                ArtworkPath = reader.IsDBNull(reader.GetOrdinal("ArtworkPath"))
                    ? string.Empty
                    : reader.GetString(reader.GetOrdinal("ArtworkPath"))
            });
        }

        return artists;
    }

    public async Task<IReadOnlyList<Genre>> GetGenresAsync()
    {
        var genres = new List<Genre>();

        using var command = _connection.CreateCommand();
        command.CommandText = """
            SELECT
                MIN(Id) as Id,
                Genre as Name,
                COUNT(*) as TrackCount
            FROM Songs
            WHERE Genre IS NOT NULL AND Genre != ''
            GROUP BY Genre
            ORDER BY Genre
            """;

        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            genres.Add(new Genre
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                Name = reader.IsDBNull(reader.GetOrdinal("Name"))
                    ? string.Empty
                    : reader.GetString(reader.GetOrdinal("Name")),
                TrackCount = reader.GetInt32(reader.GetOrdinal("TrackCount"))
            });
        }

        return genres;
    }

    public async Task<IReadOnlyList<Song>> GetRecentSongsAsync(int count = 50)
    {
        return await _songRepository.GetRecentlyPlayedAsync(count);
    }

    public async Task<IReadOnlyList<Song>> GetMostPlayedAsync(int count = 50)
    {
        return await _songRepository.GetMostPlayedAsync(count);
    }

    public async Task<Song> AddSongAsync(string filePath)
    {
        _logger.LogInformation("Adding single song: {FilePath}", filePath);

        if (!System.IO.File.Exists(filePath))
            throw new FileNotFoundException("Audio file not found.", filePath);

        if (!SupportedExtensions.Contains(Path.GetExtension(filePath)))
            throw new NotSupportedException($"File format '{Path.GetExtension(filePath)}' is not supported.");

        var song = CreateSongFromFile(filePath)
            ?? throw new InvalidOperationException($"Failed to read metadata from: {filePath}");

        return await _songRepository.AddOrUpdateAsync(song);
    }

    public async Task RemoveSongAsync(int songId)
    {
        _logger.LogInformation("Removing song with Id: {SongId}", songId);
        await _songRepository.DeleteAsync(songId);
    }

    public async Task<bool> ToggleFavoriteAsync(int songId)
    {
        bool newStatus = await _songRepository.ToggleFavoriteAsync(songId);
        var song = await _songRepository.GetByIdAsync(songId);
        if (song != null)
        {
            SongFavoriteToggled?.Invoke(this, song);
        }
        return newStatus;
    }

    public async Task<IReadOnlyList<Song>> GetFavoritesAsync()
    {
        return await _songRepository.GetFavoritesAsync();
    }

    public async Task AddSongToHistoryAsync(int songId)
    {
        await _songRepository.AddToHistoryAsync(songId);
    }

    private Song? CreateSongFromFile(string filePath)
    {
        try
        {
            var fileInfo = new FileInfo(filePath);
            var tagFile = TagLib.File.Create(filePath);
            var tag = tagFile.Tag;
            var properties = tagFile.Properties;

            var duration = properties?.Duration ?? TimeSpan.Zero;
            var bitRate = properties?.AudioBitrate ?? 0;

            string? artworkPath = null;
            if (tag.Pictures is { Length: > 0 })
            {
                var picture = tag.Pictures[0];
                if (picture.Data.Count > 0)
                {
                    var cachedPath = ArtworkCache.CacheArtworkAsync(
                        filePath,
                        picture.Data.ToArray()).GetAwaiter().GetResult();
                    artworkPath = cachedPath;
                }
            }

            var song = new Song
            {
                FilePath = filePath,
                Title = string.IsNullOrWhiteSpace(tag.Title)
                    ? Path.GetFileNameWithoutExtension(filePath)
                    : tag.Title,
                Artist = string.IsNullOrWhiteSpace(tag.JoinedPerformers)
                    ? "Unknown Artist"
                    : tag.JoinedPerformers,
                Album = string.IsNullOrWhiteSpace(tag.Album)
                    ? "Unknown Album"
                    : tag.Album,
                Genre = tag.JoinedGenres ?? string.Empty,
                TrackNumber = (int)tag.Track,
                Year = (int)tag.Year,
                Duration = duration,
                BitRate = bitRate,
                ArtworkPath = artworkPath ?? string.Empty,
                PlayCount = 0,
                DateAdded = DateTime.UtcNow,
                IsFavorite = false,
                FileFormat = Path.GetExtension(filePath).TrimStart('.').ToUpperInvariant(),
                FileSize = fileInfo.Length
            };

            tagFile.Dispose();
            return song;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "TagLib failed to read: {FilePath}", filePath);
            return null;
        }
    }

    private async Task RebuildAggregatedTablesAsync()
    {
        _logger.LogInformation("Rebuilding aggregated tables (Albums, Artists, Genres).");

        using var transaction = _connection.BeginTransaction();

        try
        {
            await ExecuteNonQueryAsync(_connection, "DELETE FROM Albums");
            await ExecuteNonQueryAsync(_connection, "DELETE FROM Artists");
            await ExecuteNonQueryAsync(_connection, "DELETE FROM Genres");

            await ExecuteNonQueryAsync(_connection, """
                INSERT INTO Albums (Name, Artist, ArtistId, ArtworkPath, Year, TrackCount, TotalDuration, DateAdded)
                SELECT
                    Album,
                    Artist,
                    -1,
                    MIN(ArtworkPath),
                    MAX(Year),
                    COUNT(*),
                    SUM(Duration),
                    MIN(DateAdded)
                FROM Songs
                WHERE Album IS NOT NULL AND Album != ''
                GROUP BY Album, Artist
                """);

            await ExecuteNonQueryAsync(_connection, """
                UPDATE Songs SET AlbumId = (
                    SELECT a.Id FROM Albums a
                    WHERE a.Name = Songs.Album AND a.Artist = Songs.Artist
                    LIMIT 1
                )
                WHERE Album IS NOT NULL AND Album != ''
                """);

            await ExecuteNonQueryAsync(_connection, """
                INSERT INTO Artists (Name, AlbumCount, TrackCount, ArtworkPath)
                SELECT
                    Artist,
                    COUNT(DISTINCT Album),
                    COUNT(*),
                    MIN(ArtworkPath)
                FROM Songs
                WHERE Artist IS NOT NULL AND Artist != ''
                GROUP BY Artist
                """);

            await ExecuteNonQueryAsync(_connection, """
                UPDATE Songs SET ArtistId = (
                    SELECT a.Id FROM Artists a
                    WHERE a.Name = Songs.Artist
                    LIMIT 1
                )
                WHERE Artist IS NOT NULL AND Artist != ''
                """);

            await ExecuteNonQueryAsync(_connection, """
                INSERT INTO Genres (Name, TrackCount)
                SELECT
                    Genre,
                    COUNT(*)
                FROM Songs
                WHERE Genre IS NOT NULL AND Genre != ''
                GROUP BY Genre
                """);

            await ExecuteNonQueryAsync(_connection, """
                UPDATE Songs SET GenreId = (
                    SELECT g.Id FROM Genres g
                    WHERE g.Name = Songs.Genre
                    LIMIT 1
                )
                WHERE Genre IS NOT NULL AND Genre != ''
                """);

            transaction.Commit();
            _logger.LogInformation("Aggregated tables rebuilt successfully.");
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            _logger.LogError(ex, "Failed to rebuild aggregated tables.");
            throw;
        }
    }

    private static async Task ExecuteNonQueryAsync(SqliteConnection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync();
    }
}
