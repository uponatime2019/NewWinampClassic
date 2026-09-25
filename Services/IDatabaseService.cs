using Microsoft.Data.Sqlite;

namespace NewWinampClassic.Services;

public interface IDatabaseService : IDisposable
{
    SqliteConnection GetConnection();
    Task InitializeAsync();
}
