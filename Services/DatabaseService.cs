using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using NewWinampClassic.Data;

namespace NewWinampClassic.Services;

public sealed class DatabaseService : IDatabaseService
{
    private readonly ILogger<DatabaseService> _logger;
    private SqliteConnection? _connection;
    private bool _isDisposed;

    private static string DatabasePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "NewWinampClassic",
        "library.db");

    private static string DatabaseDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "NewWinampClassic");

    public DatabaseService(ILogger<DatabaseService> logger)
    {
        _logger = logger;
    }

    public SqliteConnection GetConnection()
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        EnsureConnection();
        return _connection!;
    }

    public async Task InitializeAsync()
    {
        _logger.LogInformation("Initializing database service.");

        Directory.CreateDirectory(DatabaseDirectory);

        EnsureConnection();

        _logger.LogInformation("Database connection opened at: {Path}", DatabasePath);

        await DatabaseInitializer.InitializeAsync(_connection!);

        _logger.LogInformation("Database tables initialized.");
    }

    private void EnsureConnection()
    {
        if (_connection is not null)
            return;

        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = DatabasePath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared
        };

        _connection = new SqliteConnection(builder.ConnectionString);
        _connection.Open();
    }

    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;

        if (_connection is not null)
        {
            _connection.Close();
            _connection.Dispose();
            _connection = null;
        }

        _logger.LogInformation("DatabaseService disposed.");
    }
}
