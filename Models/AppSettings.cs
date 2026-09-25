using CommunityToolkit.Mvvm.ComponentModel;

namespace NewWinampClassic.Models;

public partial class AppSettings : ObservableObject
{
    [ObservableProperty] private string _theme = "System";
    [ObservableProperty] private double _volume = 1.0;
    [ObservableProperty] private bool _isMuted;
    [ObservableProperty] private bool _shuffleEnabled;
    [ObservableProperty] private RepeatMode _repeatMode = RepeatMode.None;
    [ObservableProperty] private double _playbackSpeed = 1.0;
    [ObservableProperty] private string _lastPlayedFilePath = string.Empty;
    [ObservableProperty] private double _lastPlaybackPosition;
    [ObservableProperty] private string _libraryPath = string.Empty;
    [ObservableProperty] private bool _autoScanOnStartup = true;
    [ObservableProperty] private bool _showMiniPlayer;
    [ObservableProperty] private bool _enableCrossfade;
    [ObservableProperty] private double _crossfadeDuration = 3.0;
    [ObservableProperty] private bool _enableNormalization;
    [ObservableProperty] private int _sleepTimerMinutes;
    [ObservableProperty] private string _accentColor = "Default";
    [ObservableProperty] private string _recentSearches = string.Empty;
    [ObservableProperty] private double _eq60;
    [ObservableProperty] private double _eq230;
    [ObservableProperty] private double _eq910;
    [ObservableProperty] private double _eq4k;
    [ObservableProperty] private double _eq14k;
    [ObservableProperty] private string _eqPreset = "Flat";
    [ObservableProperty] private bool _eqEnabled = true;
    [ObservableProperty] private double _eqPreamp;
    [ObservableProperty] private double _eqB0;
    [ObservableProperty] private double _eqB1;
    [ObservableProperty] private double _eqB2;
    [ObservableProperty] private double _eqB3;
    [ObservableProperty] private double _eqB4;
    [ObservableProperty] private double _eqB5;
    [ObservableProperty] private double _eqB6;
    [ObservableProperty] private double _eqB7;
    [ObservableProperty] private double _eqB8;
    [ObservableProperty] private double _eqB9;
    [ObservableProperty] private double _balance;
}
