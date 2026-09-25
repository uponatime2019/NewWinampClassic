using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NewWinampClassic.Helpers;
using NewWinampClassic.Models;
using NewWinampClassic.Services;

namespace NewWinampClassic.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    private readonly ISettingsService _settingsService;
    private readonly IThemeService _themeService;
    private readonly IMusicLibraryService _libraryService;
    private readonly IAudioPlaybackService _playbackService;

    [ObservableProperty] private string _selectedTheme = "System";
    [ObservableProperty] private double _volume = 1.0;
    [ObservableProperty] private bool _isMuted;
    [ObservableProperty] private double _playbackSpeed = 1.0;
    [ObservableProperty] private string _libraryPath = string.Empty;
    [ObservableProperty] private bool _autoScanOnStartup = true;
    [ObservableProperty] private bool _enableCrossfade;
    [ObservableProperty] private double _crossfadeDuration = 3.0;
    [ObservableProperty] private bool _enableNormalization;
    [ObservableProperty] private string _scanningStatus = string.Empty;
    [ObservableProperty] private bool _isScanning;
    [ObservableProperty] private int _scanProgress;
    [ObservableProperty] private int _scanTotal;

    // EQ
    [ObservableProperty] private double _eq60;
    [ObservableProperty] private double _eq230;
    [ObservableProperty] private double _eq910;
    [ObservableProperty] private double _eq4k;
    [ObservableProperty] private double _eq14k;
    [ObservableProperty] private string _selectedEqPreset = "Flat";

    public ObservableCollection<string> Themes { get; } = ["System", "Light", "Dark"];
    public ObservableCollection<double> SpeedOptions { get; } = [0.5, 0.75, 1.0, 1.25, 1.5, 2.0];
    public ObservableCollection<string> LibraryPaths { get; } = [];
    public ObservableCollection<string> EqPresets { get; } = ["Flat", "Bass Boost", "Treble Boost", "Rock", "Pop", "Jazz", "Classical"];

    public SettingsViewModel(ISettingsService settingsService, IThemeService themeService, IMusicLibraryService libraryService, IAudioPlaybackService playbackService)
    {
        _settingsService = settingsService;
        _themeService = themeService;
        _libraryService = libraryService;
        _playbackService = playbackService;
        Title = "Settings";

        var settings = _settingsService.Settings;
        _selectedTheme = settings.Theme;
        _volume = settings.Volume;
        _isMuted = settings.IsMuted;
        _playbackSpeed = settings.PlaybackSpeed;
        _libraryPath = settings.LibraryPath;
        _autoScanOnStartup = settings.AutoScanOnStartup;
        _enableCrossfade = settings.EnableCrossfade;
        _crossfadeDuration = settings.CrossfadeDuration;
        _enableNormalization = settings.EnableNormalization;
        _eq60 = settings.Eq60;
        _eq230 = settings.Eq230;
        _eq910 = settings.Eq910;
        _eq4k = settings.Eq4k;
        _eq14k = settings.Eq14k;
        _selectedEqPreset = settings.EqPreset;

        var paths = settings.LibraryPath.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var path in paths)
        {
            LibraryPaths.Add(path);
        }
    }

    partial void OnSelectedThemeChanged(string value)
    {
        _themeService.SetTheme(value);
        _settingsService.Settings.Theme = value;
        _ = _settingsService.SaveAsync();
    }

    partial void OnVolumeChanged(double value)
    {
        _settingsService.Settings.Volume = value;
        _ = _settingsService.SaveAsync();
    }

    partial void OnPlaybackSpeedChanged(double value)
    {
        _settingsService.Settings.PlaybackSpeed = value;
        _ = _settingsService.SaveAsync();
    }

    partial void OnLibraryPathChanged(string value)
    {
        _settingsService.Settings.LibraryPath = value;
        _ = _settingsService.SaveAsync();
    }

    partial void OnAutoScanOnStartupChanged(bool value)
    {
        _settingsService.Settings.AutoScanOnStartup = value;
        _ = _settingsService.SaveAsync();
    }

    [RelayCommand]
    private async Task AddLibraryPathAsync()
    {
        var picker = new Windows.Storage.Pickers.FolderPicker();
        picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.MusicLibrary;
        picker.FileTypeFilter.Add("*");

        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

        var folder = await picker.PickSingleFolderAsync();
        if (folder is not null)
        {
            var newPath = folder.Path;
            
            // Check child folder skipping
            var tempPaths = LibraryPaths.Concat([newPath]).ToList();
            var normalized = FolderUtility.NormalizeFolders(tempPaths);
            
            if (normalized.Contains(newPath, StringComparer.OrdinalIgnoreCase))
            {
                LibraryPaths.Add(newPath);
                SaveLibraryPaths();
                ScanningStatus = $"Added folder: {newPath}";
            }
            else
            {
                // New path is a subfolder of an existing path, skipped!
                ScanningStatus = $"Skipped: '{newPath}' is a child of an existing library folder.";
            }
        }
    }

    [RelayCommand]
    private void RemoveLibraryPath(string path)
    {
        if (LibraryPaths.Remove(path))
        {
            SaveLibraryPaths();
            ScanningStatus = $"Removed folder: {path}";
        }
    }

    private void SaveLibraryPaths()
    {
        var pathsList = FolderUtility.NormalizeFolders(LibraryPaths);
        
        // Sync collection if normalized removed anything
        if (pathsList.Count != LibraryPaths.Count)
        {
            LibraryPaths.Clear();
            foreach (var path in pathsList)
            {
                LibraryPaths.Add(path);
            }
        }
        
        LibraryPath = string.Join(';', pathsList);
    }

    [RelayCommand]
    private async Task BrowseLibraryPathAsync()
    {
        await AddLibraryPathAsync();
    }

    [RelayCommand]
    private async Task ScanLibraryAsync()
    {
        if (string.IsNullOrWhiteSpace(LibraryPath)) return;

        IsScanning = true;
        ScanningStatus = "Scanning...";

        var progress = new Progress<ScanningProgress>(p =>
        {
            ScanProgress = p.ProcessedFiles;
            ScanTotal = p.TotalFiles;
            ScanningStatus = p.IsComplete ? "Complete!" : $"Scanning: {p.CurrentFile}";
        });

        _libraryService.ScanProgress = progress;

        try
        {
            await _libraryService.ScanLibraryAsync(LibraryPath);
            ScanningStatus = $"Scan complete! {_libraryService.TotalSongs} songs found.";
        }
        catch (Exception ex)
        {
            ScanningStatus = $"Error: {ex.Message}";
        }
        finally
        {
            IsScanning = false;
        }
    }

    [RelayCommand]
    private async Task SaveSettingsAsync() => await _settingsService.SaveAsync();

    // EQ band partials — update playback service in real-time
    partial void OnEq60Changed(double value)
    {
        _settingsService.Settings.Eq60 = value;
        _playbackService.SetEqBand(0, (float)value);
        _ = _settingsService.SaveAsync();
    }
    partial void OnEq230Changed(double value)
    {
        _settingsService.Settings.Eq230 = value;
        _playbackService.SetEqBand(1, (float)value);
        _ = _settingsService.SaveAsync();
    }
    partial void OnEq910Changed(double value)
    {
        _settingsService.Settings.Eq910 = value;
        _playbackService.SetEqBand(2, (float)value);
        _ = _settingsService.SaveAsync();
    }
    partial void OnEq4kChanged(double value)
    {
        _settingsService.Settings.Eq4k = value;
        _playbackService.SetEqBand(3, (float)value);
        _ = _settingsService.SaveAsync();
    }
    partial void OnEq14kChanged(double value)
    {
        _settingsService.Settings.Eq14k = value;
        _playbackService.SetEqBand(4, (float)value);
        _ = _settingsService.SaveAsync();
    }
    partial void OnSelectedEqPresetChanged(string value)
    {
        var gains = EqPresetValues(value);
        Eq60 = gains[0]; Eq230 = gains[1]; Eq910 = gains[2]; Eq4k = gains[3]; Eq14k = gains[4];
        _settingsService.Settings.EqPreset = value;
        _ = _settingsService.SaveAsync();
    }

    public static double[] EqPresetValues(string preset) => preset switch
    {
        "Bass Boost"   => [8, 4, 0, 0, -1],
        "Treble Boost" => [-1, 0, 0, 4, 8],
        "Rock"         => [5, 3, -1, 3, 5],
        "Pop"          => [-1, 3, 4, 3, -1],
        "Jazz"         => [4, 2, -2, 2, 5],
        "Classical"    => [5, 3, 0, 3, 5],
        _              => [0, 0, 0, 0, 0]
    };
}
