using System.IO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using NewWinampClassic.Data;
using NewWinampClassic.Services;
using NewWinampClassic.ViewModels;
using Serilog;
namespace NewWinampClassic;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;
    public static MainWindow? MainWindow { get; private set; }

    public static string AppDisplayName => "New Winamp Classic";

    private IHost? _host;

    public App()
    {
        InitializeComponent();

        // Silent unhandled exception handlers to completely prevent crashes during shutdown
        UnhandledException += (s, e) =>
        {
            e.Handled = true;
            Log.Warning(e.Exception, "Unhandled exception silently handled: {Message}", e.Message);
        };

        TaskScheduler.UnobservedTaskException += (s, e) =>
        {
            e.SetObserved();
            Log.Warning(e.Exception, "Unobserved task exception silently handled.");
        };

        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            if (e.ExceptionObject is Exception ex)
            {
                Log.Warning(ex, "AppDomain unhandled exception silently handled.");
            }
        };

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.File(
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NewWinampClassic", "logs", "winamp-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7)
            .CreateLogger();

        _host = Host.CreateDefaultBuilder()
            .ConfigureServices(ConfigureServices)
            .ConfigureLogging(logging => logging.AddSerilog(dispose: true))
            .Build();

        Services = _host.Services;
    }

    private static void ConfigureServices(HostBuilderContext context, IServiceCollection services)
    {
        // Database
        services.AddSingleton<IDatabaseService, DatabaseService>();

        // Repositories
        services.AddTransient<ISongRepository>(sp =>
        {
            var db = sp.GetRequiredService<IDatabaseService>();
            return new SongRepository(db.GetConnection());
        });
        services.AddTransient<SettingsRepository>(sp =>
        {
            var db = sp.GetRequiredService<IDatabaseService>();
            return new SettingsRepository(db.GetConnection());
        });

        // Services
        services.AddSingleton<IAudioPlaybackService, AudioPlaybackService>();
        services.AddSingleton<IQueueService, QueueService>();
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<IThemeService>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<ThemeService>>();
            return new ThemeService(logger, MainWindow!);
        });
        services.AddSingleton<IMusicLibraryService>(sp =>
        {
            var db = sp.GetRequiredService<IDatabaseService>();
            var logger = sp.GetRequiredService<ILogger<MusicLibraryService>>();
            var songRepo = sp.GetRequiredService<ISongRepository>();
            return new MusicLibraryService(songRepo, logger, db.GetConnection());
        });
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<ISoundCloudService, SoundCloudService>();

        // ViewModels
        services.AddSingleton<MainViewModel>();
        services.AddTransient<HomeViewModel>();
        services.AddSingleton<DownloadsViewModel>();
        services.AddTransient<LibraryViewModel>();
        services.AddTransient<AlbumsViewModel>();
        services.AddTransient<ArtistsViewModel>();
        services.AddTransient<PlaylistsViewModel>();
        services.AddTransient<FavoritesViewModel>();
        services.AddTransient<RecentlyPlayedViewModel>();
        services.AddTransient<QueueViewModel>();
        services.AddTransient<SearchViewModel>();
        services.AddTransient<SettingsViewModel>();
    }

    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        var dbService = Services.GetRequiredService<IDatabaseService>();
        await dbService.InitializeAsync();

        var settingsService = Services.GetRequiredService<ISettingsService>();
        await settingsService.InitializeAsync();

        var libraryService = Services.GetRequiredService<IMusicLibraryService>();
        bool copiedSamples = false;

        // 1st run sample copy logic: if database is empty, copy sample songs
        if (libraryService.TotalSongs == 0)
        {
            var musicFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
            if (!string.IsNullOrWhiteSpace(musicFolder))
            {
                try
                {
                    if (!Directory.Exists(musicFolder))
                    {
                        Directory.CreateDirectory(musicFolder);
                    }

                    var sampleMusicDir = Path.Combine(AppContext.BaseDirectory, "Assets", "SampleMusic");
                    if (Directory.Exists(sampleMusicDir))
                    {
                        var sampleFiles = Directory.GetFiles(sampleMusicDir, "*.*");
                        foreach (var file in sampleFiles)
                        {
                            var destFile = Path.Combine(musicFolder, Path.GetFileName(file));
                            if (!File.Exists(destFile))
                            {
                                File.Copy(file, destFile);
                                Log.Information("First run copy: Copied sample song {FileName} to {MusicFolder}", Path.GetFileName(file), musicFolder);
                                copiedSamples = true;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Failed to copy sample songs on first run.");
                }
            }
        }

        // Default library path to MyMusic if not set
        var settings = settingsService.Settings;
        if (string.IsNullOrWhiteSpace(settings.LibraryPath))
        {
            var musicFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
            if (!string.IsNullOrWhiteSpace(musicFolder))
            {
                settings.LibraryPath = musicFolder;
                await settingsService.SaveAsync();
            }
        }

        // Auto-scan on startup if enabled and library path is set
        if ((settings.AutoScanOnStartup || copiedSamples || libraryService.TotalSongs == 0) && !string.IsNullOrWhiteSpace(settings.LibraryPath))
        {
            _ = libraryService.ScanLibraryAsync(settings.LibraryPath);
        }

        MainWindow = new MainWindow();
        MainWindow.Activate();

        Services.GetRequiredService<INavigationService>().SetFrame(MainWindow.AppContentFrame);

        // Check for file activation launch arguments
        try
        {
            var activatedArgs = Microsoft.Windows.AppLifecycle.AppInstance.GetCurrent().GetActivatedEventArgs();
            if (activatedArgs is not null && activatedArgs.Kind == Microsoft.Windows.AppLifecycle.ExtendedActivationKind.File)
            {
                if (activatedArgs.Data is Windows.ApplicationModel.Activation.IFileActivatedEventArgs fileArgs && fileArgs.Files.Count > 0)
                {
                    var file = fileArgs.Files[0];
                    var mainVm = Services.GetRequiredService<MainViewModel>();
                    _ = mainVm.PlayFilePathAsync(file.Path);
                }
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to handle file activation on startup.");
        }

        MainWindow.Closed += async (_, _) =>
        {
            try
            {
                var mainVm = Services.GetRequiredService<MainViewModel>();
                try
                {
                    await mainVm.StopAndSaveAsync();
                }
                catch (Exception)
                {
                    // Ignore errors during shutdown
                }

                if (_host is not null)
                {
                    try
                    {
                        await _host.StopAsync();
                    }
                    catch (Exception)
                    {
                        // Ignore errors during shutdown
                    }
                }

                Log.CloseAndFlush();
            }
            catch (Exception)
            {
            }
        };
    }
}
