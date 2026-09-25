using NewWinampClassic.Models;

namespace NewWinampClassic.Services;

public interface ISettingsService
{
    AppSettings Settings { get; }

    Task InitializeAsync();
    Task SaveAsync();

    event EventHandler? SettingsChanged;
}
