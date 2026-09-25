using System.Text.Json;
using Microsoft.Data.Sqlite;
using NewWinampClassic.Models;

namespace NewWinampClassic.Data;

/// <summary>
/// Simple key-value settings storage backed by the Settings SQLite table.
/// The connection is expected to be already open and managed externally.
/// </summary>
public sealed class SettingsRepository
{
    private readonly SqliteConnection _connection;
    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    public SettingsRepository(SqliteConnection connection)
    {
        _connection = connection;
    }

    // ---------------------------------------------------------------------
    // Generic get / set
    // ---------------------------------------------------------------------

    /// <summary>
    /// Retrieves the value associated with <paramref name="key"/> and
    /// deserializes it to <typeparamref name="T"/>. Returns
    /// <c>default</c> when the key does not exist.
    /// </summary>
    public T? Get<T>(string key)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT Value FROM Settings WHERE Key = @Key";
        cmd.Parameters.AddWithValue("@Key", key);

        var result = cmd.ExecuteScalar();
        if (result is not string json)
            return default;

        var type = typeof(T);

        // Fast path for simple types that are stored as plain text.
        if (type == typeof(string))
            return (T)(object)json;
        if (type == typeof(int) && int.TryParse(json, out var i))
            return (T)(object)i;
        if (type == typeof(double) && double.TryParse(json, System.Globalization.CultureInfo.InvariantCulture, out var d))
            return (T)(object)d;
        if (type == typeof(bool) && bool.TryParse(json, out var b))
            return (T)(object)b;
        if (type == typeof(float) && float.TryParse(json, System.Globalization.CultureInfo.InvariantCulture, out var f))
            return (T)(object)f;
        if (type == typeof(long) && long.TryParse(json, out var l))
            return (T)(object)l;
        if (type.IsEnum && Enum.TryParse(type, json, out var e))
            return (T)e;

        // Fall back to JSON deserialization for complex types.
        return JsonSerializer.Deserialize<T>(json, s_jsonOptions);
    }

    /// <summary>
    /// Persists a value under <paramref name="key"/> using INSERT OR REPLACE.
    /// Simple types are stored as their string representation; complex types
    /// are serialized to JSON.
    /// </summary>
    public void Set<T>(string key, T value)
    {
        if (value is null)
        {
            // Remove the key when the value is null.
            using var delCmd = _connection.CreateCommand();
            delCmd.CommandText = "DELETE FROM Settings WHERE Key = @Key";
            delCmd.Parameters.AddWithValue("@Key", key);
            delCmd.ExecuteNonQuery();
            return;
        }

        var type = typeof(T);
        string stored;

        if (type == typeof(string))
            stored = (string)(object)value;
        else if (type.IsEnum)
            stored = value.ToString()!;
        else if (type == typeof(bool)
              || type == typeof(int)
              || type == typeof(double)
              || type == typeof(float)
              || type == typeof(long))
            stored = Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture)!;
        else
            stored = JsonSerializer.Serialize(value, s_jsonOptions);

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
            INSERT OR REPLACE INTO Settings (Key, Value)
            VALUES (@Key, @Value)
            """;
        cmd.Parameters.AddWithValue("@Key", key);
        cmd.Parameters.AddWithValue("@Value", stored);
        cmd.ExecuteNonQuery();
    }

    // ---------------------------------------------------------------------
    // AppSettings convenience methods
    // ---------------------------------------------------------------------

    /// <summary>
    /// Loads all settings from the database and returns a fully populated
    /// <see cref="AppSettings"/> instance.
    /// </summary>
    public AppSettings LoadSettings()
    {
        var settings = new AppSettings();

        LoadValue("Theme",                settings.Theme,               v => settings.Theme = v);
        LoadValue("Volume",               settings.Volume,              v => settings.Volume = v);
        LoadValue("IsMuted",              settings.IsMuted,             v => settings.IsMuted = v);
        LoadValue("ShuffleEnabled",       settings.ShuffleEnabled,      v => settings.ShuffleEnabled = v);
        LoadValue("RepeatMode",           settings.RepeatMode,          v => settings.RepeatMode = v);
        LoadValue("PlaybackSpeed",        settings.PlaybackSpeed,       v => settings.PlaybackSpeed = v);
        LoadValue("LastPlayedFilePath",   settings.LastPlayedFilePath,  v => settings.LastPlayedFilePath = v);
        LoadValue("LastPlaybackPosition", settings.LastPlaybackPosition, v => settings.LastPlaybackPosition = v);
        LoadValue("LibraryPath",          settings.LibraryPath,         v => settings.LibraryPath = v);
        LoadValue("AutoScanOnStartup",    settings.AutoScanOnStartup,   v => settings.AutoScanOnStartup = v);
        LoadValue("ShowMiniPlayer",       settings.ShowMiniPlayer,      v => settings.ShowMiniPlayer = v);
        LoadValue("EnableCrossfade",      settings.EnableCrossfade,     v => settings.EnableCrossfade = v);
        LoadValue("CrossfadeDuration",    settings.CrossfadeDuration,   v => settings.CrossfadeDuration = v);
        LoadValue("EnableNormalization",  settings.EnableNormalization, v => settings.EnableNormalization = v);
        LoadValue("SleepTimerMinutes",    settings.SleepTimerMinutes,   v => settings.SleepTimerMinutes = v);
        LoadValue("AccentColor",          settings.AccentColor,         v => settings.AccentColor = v);
        LoadValue("RecentSearches",       settings.RecentSearches,      v => settings.RecentSearches = v);
        LoadValue("Eq60",                settings.Eq60,               v => settings.Eq60 = v);
        LoadValue("Eq230",               settings.Eq230,              v => settings.Eq230 = v);
        LoadValue("Eq910",               settings.Eq910,              v => settings.Eq910 = v);
        LoadValue("Eq4k",                settings.Eq4k,               v => settings.Eq4k = v);
        LoadValue("Eq14k",               settings.Eq14k,              v => settings.Eq14k = v);
        LoadValue("EqPreset",            settings.EqPreset,           v => settings.EqPreset = v);
        LoadValue("EqEnabled",           settings.EqEnabled,          v => settings.EqEnabled = v);
        LoadValue("EqPreamp",            settings.EqPreamp,           v => settings.EqPreamp = v);
        LoadValue("EqB0",                settings.EqB0,               v => settings.EqB0 = v);
        LoadValue("EqB1",                settings.EqB1,               v => settings.EqB1 = v);
        LoadValue("EqB2",                settings.EqB2,               v => settings.EqB2 = v);
        LoadValue("EqB3",                settings.EqB3,               v => settings.EqB3 = v);
        LoadValue("EqB4",                settings.EqB4,               v => settings.EqB4 = v);
        LoadValue("EqB5",                settings.EqB5,               v => settings.EqB5 = v);
        LoadValue("EqB6",                settings.EqB6,               v => settings.EqB6 = v);
        LoadValue("EqB7",                settings.EqB7,               v => settings.EqB7 = v);
        LoadValue("EqB8",                settings.EqB8,               v => settings.EqB8 = v);
        LoadValue("EqB9",                settings.EqB9,               v => settings.EqB9 = v);
        LoadValue("Balance",             settings.Balance,            v => settings.Balance = v);

        return settings;
    }

    public bool HasKey(string key)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT COUNT(1) FROM Settings WHERE Key = @Key";
        cmd.Parameters.AddWithValue("@Key", key);
        return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
    }

    /// <summary>
    /// Reads a setting from the database and applies it via
    /// <paramref name="apply"/>. Falls back to
    /// <paramref name="defaultValue"/> when the key is absent.
    /// </summary>
    private void LoadValue<T>(string key, T defaultValue, Action<T> apply)
    {
        if (HasKey(key))
        {
            var value = Get<T>(key);
            apply(value!);
        }
        else
        {
            apply(defaultValue);
        }
    }

    /// <summary>
    /// Persists every property of <paramref name="settings"/> as individual
    /// key-value rows in the Settings table.
    /// </summary>
    public void SaveSettings(AppSettings settings)
    {
        using var transaction = _connection.BeginTransaction();

        try
        {
            Set("Theme",                settings.Theme);
            Set("Volume",               settings.Volume);
            Set("IsMuted",              settings.IsMuted);
            Set("ShuffleEnabled",       settings.ShuffleEnabled);
            Set("RepeatMode",           settings.RepeatMode);
            Set("PlaybackSpeed",        settings.PlaybackSpeed);
            Set("LastPlayedFilePath",   settings.LastPlayedFilePath);
            Set("LastPlaybackPosition", settings.LastPlaybackPosition);
            Set("LibraryPath",          settings.LibraryPath);
            Set("AutoScanOnStartup",    settings.AutoScanOnStartup);
            Set("ShowMiniPlayer",       settings.ShowMiniPlayer);
            Set("EnableCrossfade",      settings.EnableCrossfade);
            Set("CrossfadeDuration",    settings.CrossfadeDuration);
            Set("EnableNormalization",  settings.EnableNormalization);
            Set("SleepTimerMinutes",    settings.SleepTimerMinutes);
            Set("AccentColor",          settings.AccentColor);
            Set("RecentSearches",       settings.RecentSearches);
            Set("Eq60",                settings.Eq60);
            Set("Eq230",               settings.Eq230);
            Set("Eq910",               settings.Eq910);
            Set("Eq4k",                settings.Eq4k);
            Set("Eq14k",               settings.Eq14k);
            Set("EqPreset",            settings.EqPreset);
            Set("EqEnabled",           settings.EqEnabled);
            Set("EqPreamp",            settings.EqPreamp);
            Set("EqB0",                settings.EqB0);
            Set("EqB1",                settings.EqB1);
            Set("EqB2",                settings.EqB2);
            Set("EqB3",                settings.EqB3);
            Set("EqB4",                settings.EqB4);
            Set("EqB5",                settings.EqB5);
            Set("EqB6",                settings.EqB6);
            Set("EqB7",                settings.EqB7);
            Set("EqB8",                settings.EqB8);
            Set("EqB9",                settings.EqB9);
            Set("Balance",             settings.Balance);

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }
}
