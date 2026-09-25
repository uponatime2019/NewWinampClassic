using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Logging;
using NewWinampClassic.Data;
using NewWinampClassic.Models;

namespace NewWinampClassic.Services;

public sealed class SettingsService : ISettingsService
{
    private readonly SettingsRepository _repository;
    private readonly ILogger<SettingsService> _logger;

    public AppSettings Settings { get; private set; } = new();

    public event EventHandler? SettingsChanged;

    public SettingsService(SettingsRepository repository, ILogger<SettingsService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task InitializeAsync()
    {
        _logger.LogInformation("Initializing settings service.");
        Settings = await Task.Run(() => _repository.LoadSettings());

        // Check if we need to initialize default library folders on first run
        var isFirstRun = !_repository.Get<bool>("DefaultsInitialized");
        if (isFirstRun)
        {
            var musicFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
            var videosFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);

            var defaultPaths = new List<string>();
            if (!string.IsNullOrWhiteSpace(musicFolder) && Directory.Exists(musicFolder))
            {
                defaultPaths.Add(musicFolder);
            }
            if (!string.IsNullOrWhiteSpace(videosFolder) && Directory.Exists(videosFolder))
            {
                defaultPaths.Add(videosFolder);
            }

            if (defaultPaths.Count > 0)
            {
                Settings.LibraryPath = string.Join(";", defaultPaths);
            }

            _repository.Set("DefaultsInitialized", true);
            await SaveAsync();
            _logger.LogInformation("First run: Initialized default library paths to: {Paths}", Settings.LibraryPath);
        }

        _logger.LogInformation("Settings loaded. Theme={Theme}, Volume={Volume}", Settings.Theme, Settings.Volume);
    }

    public async Task SaveAsync()
    {
        _logger.LogInformation("Saving settings.");
        await Task.Run(() => _repository.SaveSettings(Settings));
        SettingsChanged?.Invoke(this, EventArgs.Empty);
        _logger.LogInformation("Settings saved and event raised.");
    }
}
