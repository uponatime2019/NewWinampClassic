using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using NewWinampClassic.Controls;
using NewWinampClassic.Helpers;
using NewWinampClassic.Models;
using NewWinampClassic.Services;
using NewWinampClassic.ViewModels;
using WinRT.Interop;

namespace NewWinampClassic;

public sealed partial class MainWindow : Window
{
    private AppWindow _appWindow = null!;
    private MainViewModel _viewModel = null!;
    private IAudioPlaybackService _playbackService = null!;
    private IQueueService _queueService = null!;
    private ISettingsService _settingsService = null!;
    private IMusicLibraryService _libraryService = null!;

    private bool _isClosing;
    private bool _showRemaining;
    private bool _isSeekDragging;
    private bool _isDraggingWindow;
    private POINT _dragStartCursor;
    private Windows.Graphics.PointInt32 _dragStartWindowPos;
    private bool _doubleSize;
    private bool _mlInitialized;
    private bool _initializing;
    private string _tickerText = "*** WINAMP CLASSIC ***  It really whips the llama's ass!";
    private readonly DispatcherQueueTimer _tickerTimer;
    private readonly ClassicSlider[] _eqSliders = new ClassicSlider[10];

    private static readonly string[] AudioExtensions = [".mp3", ".flac", ".wav", ".ogg", ".m4a", ".aac", ".wma", ".opus", ".aiff"];

    private static readonly (string Name, double[] Gains)[] EqPresets =
    [
        ("Flat",              [0, 0, 0, 0, 0, 0, 0, 0, 0, 0]),
        ("Rock",              [8, 4, -5, -8, -3, 4, 8, 11, 11, 12]),
        ("Pop",               [-1, 4, 7, 8, 5, 0, -2, -2, -1, -1]),
        ("Classical",         [0, 0, 0, 0, 0, 0, -7, -7, -7, -9]),
        ("Club",              [0, 0, 8, -6, -6, -6, 8, 8, 8, 8]),
        ("Dance",             [9, 7, 2, 0, 0, -5, -7, -7, 0, 0]),
        ("Full Bass",         [-8, 9, 9, 5, 1, -4, -8, -10, -11, -11]),
        ("Full Bass & Treble",[7, 6, 2, 0, -1, 1, 8, 11, 12, 12]),
        ("Full Treble",       [-9, -9, -9, -4, 2, 11, 16, 16, 16, 16]),
        ("Large Hall",        [10, 10, 5, 5, 0, -3, -3, -3, 0, 0]),
        ("Live",              [-4, 0, 4, 5, 5, 5, 4, 2, 2, 2]),
        ("Party",             [7, 7, 0, 0, 0, 0, 0, 0, 7, 7]),
        ("Reggae",            [0, 0, 0, -5, 0, 6, 6, 0, 0, 0]),
        ("Ska",               [-2, -4, -4, 0, 4, 7, 9, 9, 11, 9]),
        ("Soft Rock",         [4, 4, 2, 0, -2, 0, 4, 8, 10, 11]),
        ("Techno",            [8, 5, 0, -5, -4, 0, 8, 9, 9, 9]),
    ];

    private static readonly string[] EqBandLabels = ["60", "170", "310", "600", "1K", "3K", "6K", "12K", "14K", "16K"];

    [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true, CharSet = System.Runtime.InteropServices.CharSet.Auto)]
    private static extern IntPtr LoadImage(IntPtr hinst, string lpszName, uint uType, int cxDesired, int cyDesired, uint fuLoad);

    [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool ReleaseCapture();

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(IntPtr hwnd);

    [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool AdjustWindowRect(ref RECT lpRect, int dwStyle, bool bMenu);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left, Top, Right, Bottom;
    }

    public Frame AppContentFrame => ContentFrame;

    public MainWindow()
    {
        InitializeComponent();
        Title = App.AppDisplayName;

        _viewModel = App.Services.GetRequiredService<MainViewModel>();
        _playbackService = App.Services.GetRequiredService<IAudioPlaybackService>();
        _queueService = App.Services.GetRequiredService<IQueueService>();
        _settingsService = App.Services.GetRequiredService<ISettingsService>();
        _libraryService = App.Services.GetRequiredService<IMusicLibraryService>();

        SetupWindowChrome();
        BuildEqBands();
        BuildEqBands();
        BuildPresetFlyout();
        InitFromSettings();
        WireEvents();
        InitPlaylist();

        _tickerTimer = DispatcherQueue.CreateTimer();
        _tickerTimer.Interval = TimeSpan.FromMilliseconds(40);
        _tickerTimer.Tick += (_, _) => AdvanceTicker();
        _tickerTimer.Start();

        Activated += (_, _) =>
        {
            if (ContentFrame.Content is null && !MlWin.Visibility.Equals(Visibility.Visible))
                ContentFrame.Navigate(typeof(Views.HomePage));
        };
    }

    // ---------------------------------------------------------------
    // Window chrome
    // ---------------------------------------------------------------

    private void SetupWindowChrome()
    {
        var hwnd = WindowNative.GetWindowHandle(this);
        var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
        _appWindow = AppWindow.GetFromWindowId(windowId);
        _appWindow.Closing += AppWindow_Closing;

        if (_appWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsResizable = false;
            presenter.IsMaximizable = false;
            try
            {
                presenter.SetBorderAndTitleBar(true, false);
            }
            catch (Exception)
            {
                // Older runtime without the API — fall back to default chrome.
            }
        }

        try
        {
            var iconPath = System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "AppIcon.ico");
            if (System.IO.File.Exists(iconPath))
            {
                IntPtr hIcon = LoadImage(IntPtr.Zero, iconPath, 1, 0, 0, 0x10);
                if (hIcon != IntPtr.Zero)
                {
                    SendMessage(hwnd, 0x0080, new IntPtr(0), hIcon);
                    SendMessage(hwnd, 0x0080, new IntPtr(1), hIcon);
                }
                _appWindow.SetIcon(iconPath);
            }
        }
        catch (Exception) { }

        AttachDrag(MainTitleBar);
        AttachDrag(TitleDragArea);
        AttachDrag(EqTitleBar);
        AttachDrag(PlTitleBar);
        AttachDrag(MlTitleBar);
        RootGrid.Loaded += (_, _) =>
        {
            ResizeWindow();
            UpdateEqGraph();
        };
    }

    private void AttachDrag(FrameworkElement? element)
    {
        if (element == null) return;

        element.PointerPressed += (s, e) =>
        {
            var props = e.GetCurrentPoint(element).Properties;
            if (props.IsLeftButtonPressed)
            {
                if (element.CapturePointer(e.Pointer))
                {
                    _isDraggingWindow = true;
                    GetCursorPos(out _dragStartCursor);
                    if (_appWindow != null)
                    {
                        _dragStartWindowPos = _appWindow.Position;
                    }
                    e.Handled = true;
                }
            }
        };

        element.PointerMoved += (s, e) =>
        {
            if (_isDraggingWindow && _appWindow != null)
            {
                if (GetCursorPos(out POINT currentCursor))
                {
                    int dx = currentCursor.X - _dragStartCursor.X;
                    int dy = currentCursor.Y - _dragStartCursor.Y;
                    _appWindow.Move(new Windows.Graphics.PointInt32(_dragStartWindowPos.X + dx, _dragStartWindowPos.Y + dy));
                    e.Handled = true;
                }
            }
        };

        element.PointerReleased += (s, e) =>
        {
            if (_isDraggingWindow)
            {
                _isDraggingWindow = false;
                element.ReleasePointerCapture(e.Pointer);
                e.Handled = true;
            }
        };

        element.PointerCanceled += (s, e) =>
        {
            if (_isDraggingWindow)
            {
                _isDraggingWindow = false;
                element.ReleasePointerCapture(e.Pointer);
                e.Handled = true;
            }
        };

        element.PointerCaptureLost += (s, e) =>
        {
            _isDraggingWindow = false;
        };
    }

    private void ResizeWindow()
    {
        try
        {
            var hwnd = WindowNative.GetWindowHandle(this);
            var scale = GetDpiForWindow(hwnd) / 96.0;
            var ds = _doubleSize ? 1.5 : 1.0;

            var logicalH = 232
                + (EqWin.Visibility == Visibility.Visible ? 234 : 0)
                + (PlWin.Visibility == Visibility.Visible ? 330 : 0)
                + (MlWin.Visibility == Visibility.Visible ? 482 : 0);

            var w = (int)Math.Ceiling(552 * scale * ds);
            var h = (int)Math.Ceiling(logicalH * scale * ds);

            var style = GetWindowLong(hwnd, -16);
            var rect = new RECT { Right = w, Bottom = h };
            AdjustWindowRect(ref rect, style, false);
            _appWindow.Resize(new Windows.Graphics.SizeInt32(rect.Right - rect.Left, rect.Bottom - rect.Top));
        }
        catch (Exception)
        {
        }
    }

    // ---------------------------------------------------------------
    // Initialization
    // ---------------------------------------------------------------

    private void InitFromSettings()
    {
        _initializing = true;
        var settings = _settingsService.Settings;
        VolumeSlider.Value = settings.Volume * 100;
        BalanceSlider.Value = settings.Balance * 50;
        _playbackService.Balance = settings.Balance;

        BtnShuffle.IsChecked = _queueService.IsShuffleEnabled;
        BtnRepeat.IsChecked = _queueService.RepeatMode != RepeatMode.None;

        BtnEqOn.IsChecked = settings.EqEnabled;
        PreampSlider.Value = settings.EqPreamp;
        var bandValues = new[]
        {
            settings.EqB0, settings.EqB1, settings.EqB2, settings.EqB3, settings.EqB4,
            settings.EqB5, settings.EqB6, settings.EqB7, settings.EqB8, settings.EqB9,
        };
        for (var i = 0; i < 10; i++)
        {
            if (_eqSliders[i] is not null)
                _eqSliders[i].Value = bandValues[i];
        }

        ApplyEqToEngine();
        UpdateEqGraph();
        _initializing = false;
    }

    private void BuildEqBands()
    {
        for (var i = 0; i < 10; i++)
        {
            var column = new StackPanel { Orientation = Orientation.Vertical, Spacing = 2, Width = 46 };
            var valueText = new TextBlock
            {
                Text = "+0.0",
                FontFamily = new FontFamily("Consolas"),
                FontSize = 10,
                Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 0x2B, 0xE2, 0x2B)),
                HorizontalAlignment = HorizontalAlignment.Center,
            };
            var slider = new ClassicSlider
            {
                Orientation = Orientation.Vertical,
                Width = 30,
                Height = 106,
                Minimum = -12,
                Maximum = 12,
                Step = 0.5,
                ShowFill = false,
            };
            ToolTipService.SetToolTip(slider, $"{EqBandLabels[i]} Hz");
            var index = i;
            slider.ValueChanged += (_, _) =>
            {
                valueText.Text = slider.Value >= 0 ? $"+{slider.Value:F1}" : $"{slider.Value:F1}";
                OnEqBandChanged(index, slider.Value);
            };

            var freqLabel = new TextBlock
            {
                Text = EqBandLabels[i],
                FontFamily = new FontFamily("Tahoma"),
                FontSize = 10,
                Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 0xA8, 0xB2, 0xA8)),
                HorizontalAlignment = HorizontalAlignment.Center,
            };

            column.Children.Add(valueText);
            column.Children.Add(slider);
            column.Children.Add(freqLabel);
            EqBandsHost.Children.Add(column);
            _eqSliders[i] = slider;
        }
    }

    private void BuildPresetFlyout()
    {
        foreach (var (name, _) in EqPresets)
        {
            var presetName = name;
            var item = new MenuFlyoutItem { Text = name };
            item.Click += (_, _) => ApplyPreset(presetName);
            EqPresetsFlyout.Items.Add(item);
        }
    }

    private void WireEvents()
    {
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;

        _playbackService.FftCalculated += (s, e) =>
            DispatcherQueue.TryEnqueue(() => Spectrum.UpdateSpectrum(e.Result));

        _playbackService.PlaybackStateChanged += (s, e) =>
            DispatcherQueue.TryEnqueue(() =>
            {
                if (e.NewState == PlaybackState.Stopped)
                {
                    Spectrum.Reset();
                    BitrateDisplay.Text = "128";
                    KhzDisplay.Text = "44";
                }
                else if (e.NewState == PlaybackState.Playing)
                {
                    UpdateAudioInfo();
                    if (BtnEqAuto.IsChecked == true)
                        ApplyAutoPreset();
                }
            });

        _queueService.QueueChanged += (s, e) => DispatcherQueue.TryEnqueue(UpdatePlaylistUi);
        _queueService.CurrentSongChanged += (s, e) => DispatcherQueue.TryEnqueue(UpdatePlaylistUi);

        Seekbar.PointerPressed += (_, _) => _isSeekDragging = true;
        Seekbar.PointerReleased += (_, _) => _isSeekDragging = false;
        Seekbar.PointerCanceled += (_, _) => _isSeekDragging = false;

        VolumeSlider.ValueChanged += (s, v) =>
        {
            _viewModel.Volume = v / 100.0;
            _settingsService.Settings.Volume = v / 100.0;
        };

        BalanceSlider.ValueChanged += (s, v) =>
        {
            var balance = v / 50.0;
            _playbackService.Balance = balance;
            _settingsService.Settings.Balance = balance;
        };

        Seekbar.ValueChanged += (s, v) =>
        {
            if (_isSeekDragging)
                TimeDisplay.Text = FormatTime(TimeSpan.FromSeconds(v));
        };
        Seekbar.Commit += async (s, v) =>
        {
            _isSeekDragging = false;
            if (_viewModel.SeekCommand.CanExecute(v))
                await _viewModel.SeekCommand.ExecuteAsync(v);
        };

        PreampSlider.ValueChanged += (_, _) =>
        {
            _settingsService.Settings.EqPreamp = PreampSlider.Value;
            ApplyEqToEngine();
            _ = _settingsService.SaveAsync();
        };

        RootGrid.KeyDown += (s, e) =>
        {
            if (e.Key == Windows.System.VirtualKey.Space)
            {
                _ = _viewModel.PlayPauseCommand.ExecuteAsync(null);
                e.Handled = true;
            }
        };
    }

    private void InitPlaylist()
    {
        PlaylistList.ItemsSource = _queueService.Queue;
        PlaylistList.ContainerContentChanging += OnPlaylistContainerChanging;
        _libraryService.LibraryScanCompleted += (s, e) => DispatcherQueue.TryEnqueue(async () => await LoadStartupPlaylistAsync(refreshOnly: true));
        UpdatePlaylistUi();
        _ = LoadStartupPlaylistAsync();
    }

    private async Task LoadStartupPlaylistAsync(bool refreshOnly = false)
    {
        try
        {
            if (_queueService.Queue.Count > 0 && !refreshOnly)
            {
                UpdatePlaylistUi();
                return;
            }

            var songs = await _libraryService.GetAllSongsAsync();
            if (songs.Count > 0)
            {
                var currentSong = _queueService.CurrentSong;
                var currentIndex = currentSong != null
                    ? songs.ToList().FindIndex(s => s.FilePath.Equals(currentSong.FilePath, StringComparison.OrdinalIgnoreCase))
                    : 0;
                if (currentIndex < 0) currentIndex = 0;

                _queueService.SetQueue(songs, currentIndex);
            }
            else
            {
                var settings = _settingsService.Settings;
                var rawFolders = (settings.LibraryPath ?? string.Empty)
                    .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .ToList();

                if (rawFolders.Count == 0)
                {
                    var myMusic = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
                    if (!string.IsNullOrWhiteSpace(myMusic) && System.IO.Directory.Exists(myMusic))
                    {
                        rawFolders.Add(myMusic);
                    }
                }

                var folders = FolderUtility.NormalizeFolders(rawFolders);
                var loadedSongs = new List<Song>();
                foreach (var folder in folders)
                {
                    if (!System.IO.Directory.Exists(folder)) continue;
                    foreach (var ext in AudioExtensions)
                    {
                        try
                        {
                            foreach (var file in System.IO.Directory.EnumerateFiles(folder, $"*{ext}", System.IO.SearchOption.AllDirectories))
                            {
                                try
                                {
                                    var song = await _libraryService.AddSongAsync(file);
                                    if (song != null && !loadedSongs.Any(s => s.FilePath.Equals(song.FilePath, StringComparison.OrdinalIgnoreCase)))
                                    {
                                        loadedSongs.Add(song);
                                    }
                                }
                                catch { }
                            }
                        }
                        catch { }
                    }
                }

                if (loadedSongs.Count > 0)
                {
                    _queueService.SetQueue(loadedSongs, 0);
                }
            }

            UpdatePlaylistUi();
        }
        catch (Exception ex)
        {
            Serilog.Log.Warning(ex, "Failed to load startup playlist from scan folders.");
        }
    }

    // ---------------------------------------------------------------
    // View model plumbing
    // ---------------------------------------------------------------

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            switch (e.PropertyName)
            {
                case nameof(MainViewModel.CurrentPosition):
                    if (!_isSeekDragging)
                    {
                        TimeDisplay.Text = FormatDisplayTime();
                        Seekbar.Value = _viewModel.CurrentPosition.TotalSeconds;
                    }
                    break;
                case nameof(MainViewModel.Duration):
                    Seekbar.Maximum = Math.Max(1, _viewModel.Duration.TotalSeconds);
                    break;
                case nameof(MainViewModel.CurrentSong):
                    OnSongChanged(_viewModel.CurrentSong);
                    break;
            }
        });
    }

    private void OnSongChanged(Song? song)
    {
        if (song is null)
        {
            _tickerText = "*** WINAMP CLASSIC ***  It really whips the llama's ass!";
        }
        else
        {
            var length = song.Duration > TimeSpan.Zero ? song.Duration.ToString(@"mm\:ss") : "";
            _tickerText = $"*** WINAMP CLASSIC ***   .   {song.DisplayArtist} - {song.DisplayTitle}" +
                          (length.Length > 0 ? $"   .   [{length}]   .   " : "   .   ") +
                          "It really whips the llama's ass!      ";
        }
        TickerText.Text = _tickerText;
        TickerText.UpdateLayout();
        Canvas.SetLeft(TickerText, TickerCanvas.ActualWidth);
        BitrateDisplay.Text = song is not null && song.BitRate > 0 ? song.BitRate.ToString() : "128";
        UpdateAudioInfo();
        UpdatePlaylistUi();
    }

    private void UpdateAudioInfo()
    {
        KhzDisplay.Text = (_playbackService.SampleRate / 1000).ToString();
        var channels = _playbackService.Channels;
        var on = Windows.UI.Color.FromArgb(255, 0x2B, 0xE2, 0x2B);
        var off = Windows.UI.Color.FromArgb(255, 0x0E, 0x3A, 0x0E);
        StereoLabel.Foreground = new SolidColorBrush(channels >= 2 ? on : off);
        MonoLabel.Foreground = new SolidColorBrush(channels == 1 ? on : off);
    }

    private string FormatDisplayTime()
    {
        var t = _showRemaining
            ? _viewModel.Duration - _viewModel.CurrentPosition
            : _viewModel.CurrentPosition;
        if (t < TimeSpan.Zero)
            t = TimeSpan.Zero;
        return _showRemaining ? "-" + FormatTime(t) : FormatTime(t);
    }

    private static string FormatTime(TimeSpan t)
        => $"{(int)t.TotalMinutes:D2}:{t.Seconds:D2}";

    // ---------------------------------------------------------------
    // Ticker marquee
    // ---------------------------------------------------------------

    private void AdvanceTicker()
    {
        var x = Canvas.GetLeft(TickerText) - 2;
        if (TickerText.ActualWidth > 0 && x < -TickerText.ActualWidth)
            x = TickerCanvas.ActualWidth;
        else if (TickerText.ActualWidth == 0)
            x = TickerCanvas.ActualWidth;
        Canvas.SetLeft(TickerText, x);
    }

    // ---------------------------------------------------------------
    // Playlist editor
    // ---------------------------------------------------------------

    private void OnPlaylistContainerChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
    {
        if (args.ItemContainer.ContentTemplateRoot is not Grid root)
            return;

        if (root.Children.FirstOrDefault(c => c is TextBlock { Name: "IndexText" }) is TextBlock indexText)
            indexText.Text = $"{args.ItemIndex + 1}.";

        var song = args.Item as Song;
        var isCurrent = song is not null && _queueService.CurrentSong is { } cur && cur.Id == song.Id && cur.FilePath == song.FilePath;

        if (root.Children.OfType<TextBlock>().Skip(1).FirstOrDefault() is TextBlock titleText)
        {
            titleText.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255,
                isCurrent ? (byte)0xB8 : (byte)0x00,
                isCurrent ? (byte)0xFF : (byte)0xE8,
                (byte)0x00));
            titleText.FontWeight = isCurrent ? new Windows.UI.Text.FontWeight { Weight = 700 } : new Windows.UI.Text.FontWeight { Weight = 400 };
        }
    }

    private void UpdatePlaylistUi()
    {
        var count = _queueService.Queue.Count;
        var total = TimeSpan.FromSeconds(_queueService.Queue.Sum(s => s.Duration.TotalSeconds));

        PlStatusText.Text = $"{count} item{(count == 1 ? "" : "s")}  {total:mm\\:ss}";
        var minutes = (int)total.TotalMinutes;
        PlTotalTime.Text = $"{minutes}:{total.Seconds:D2}";

        // refresh per-row current-song highlight
        foreach (var container in PlaylistList.ItemsPanelRoot?.Children.OfType<SelectorItem>() ?? Enumerable.Empty<SelectorItem>())
        {
            if (container.ContentTemplateRoot is Grid root && container.Content is Song song)
            {
                var isCurrent = _queueService.CurrentSong is { } cur && cur.Id == song.Id;
                if (root.Children.OfType<TextBlock>().Skip(1).FirstOrDefault() is TextBlock titleText)
                {
                    titleText.Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255,
                        isCurrent ? (byte)0xB8 : (byte)0x00,
                        isCurrent ? (byte)0xFF : (byte)0xE8,
                        (byte)0x00));
                    titleText.FontWeight = isCurrent ? new Windows.UI.Text.FontWeight { Weight = 700 } : new Windows.UI.Text.FontWeight { Weight = 400 };
                }
            }
        }
    }

    private async void PlaylistList_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        if ((e.OriginalSource as FrameworkElement)?.DataContext is Song song)
        {
            var index = _queueService.Queue.IndexOf(song);
            await _viewModel.PlayQueueAtAsync(index);
        }
    }

    private void PlaylistList_DragItemsCompleted(ListViewBase sender, DragItemsCompletedEventArgs args)
    {
        // ListView reordered the ObservableCollection; resync the queue's current index.
        var current = _queueService.CurrentSong;
        if (current is null)
            return;
        var idx = _queueService.Queue.IndexOf(current);
        if (idx >= 0)
            _queueService.PlayAt(idx);
        UpdatePlaylistUi();
    }

    private void PlRemoveSelected_Click(object sender, RoutedEventArgs e)
    {
        var selected = PlaylistList.SelectedItems.Cast<Song>().ToList();
        foreach (var song in selected)
        {
            var idx = _queueService.Queue.IndexOf(song);
            if (idx >= 0)
                _queueService.RemoveFromQueue(idx);
        }
    }

    private void PlCrop_Click(object sender, RoutedEventArgs e)
    {
        var selected = PlaylistList.SelectedItems.Cast<Song>().ToHashSet();
        var keep = _queueService.Queue.Where(selected.Contains).ToList();
        var current = _queueService.CurrentSong;
        var currentIndex = current is not null ? keep.IndexOf(current) : -1;
        _queueService.SetQueue(keep, Math.Max(0, currentIndex));
    }

    private void PlClear_Click(object sender, RoutedEventArgs e)
    {
        _queueService.Clear();
        PlaylistList.SelectedItems.Clear();
    }

    private void PlSelectAll_Click(object sender, RoutedEventArgs e) => PlaylistList.SelectAll();

    private void PlSelectNone_Click(object sender, RoutedEventArgs e) => PlaylistList.SelectedItems.Clear();

    private void PlSelectInvert_Click(object sender, RoutedEventArgs e)
    {
        var selected = PlaylistList.SelectedItems.Cast<Song>().ToHashSet();
        PlaylistList.SelectedItems.Clear();
        foreach (var song in _queueService.Queue)
        {
            if (!selected.Contains(song))
                PlaylistList.SelectedItems.Add(song);
        }
    }

    private void PlSortTitle_Click(object sender, RoutedEventArgs e) => SortQueueConcrete((a, b) => string.CompareOrdinal(a.DisplayTitle, b.DisplayTitle));
    private void PlSortArtist_Click(object sender, RoutedEventArgs e) => SortQueueConcrete((a, b) => string.CompareOrdinal(a.DisplayArtist, b.DisplayArtist));
    private void PlSortPath_Click(object sender, RoutedEventArgs e) => SortQueueConcrete((a, b) => string.CompareOrdinal(a.FilePath, b.FilePath));

    private void SortQueueConcrete(Comparison<Song> comparison)
    {
        var list = _queueService.Queue.ToList();
        list.Sort(comparison);
        RequeuePreservingCurrent(list);
    }

    private void PlRandomize_Click(object sender, RoutedEventArgs e)
    {
        var list = _queueService.Queue.ToList();
        var rng = Random.Shared;
        for (var i = list.Count - 1; i > 0; i--)
        {
            var j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
        RequeuePreservingCurrent(list);
    }

    private void RequeuePreservingCurrent(List<Song> list)
    {
        var current = _queueService.CurrentSong;
        var idx = current is not null ? list.IndexOf(current) : 0;
        _queueService.SetQueue(list, Math.Max(0, idx));
    }

    private void PlRemoveDead_Click(object sender, RoutedEventArgs e)
    {
        var alive = _queueService.Queue.Where(s => System.IO.File.Exists(s.FilePath)).ToList();
        if (alive.Count != _queueService.Queue.Count)
            RequeuePreservingCurrent(alive);
    }

    // ---------------------------------------------------------------
    // File open / add
    // ---------------------------------------------------------------

    private async void MenuOpenFiles_Click(object sender, RoutedEventArgs e)
    {
        var hwnd = WindowNative.GetWindowHandle(this);
        var picker = new Windows.Storage.Pickers.FileOpenPicker();
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
        picker.FileTypeFilter.Add("*");
        foreach (var ext in AudioExtensions)
            picker.FileTypeFilter.Add(ext);

        var files = await picker.PickMultipleFilesAsync();
        if (files.Count == 0)
            return;

        Song? first = null;
        foreach (var file in files)
        {
            var song = await _libraryService.AddSongAsync(file.Path);
            if (song is not null)
            {
                _queueService.AddToQueue(song);
                first ??= song;
            }
        }

        if (first is not null && _viewModel.PlaybackState == PlaybackState.Stopped)
            await _viewModel.PlaySongAsync(first);
    }

    private async void MenuAddFolder_Click(object sender, RoutedEventArgs e)
    {
        var hwnd = WindowNative.GetWindowHandle(this);
        var picker = new Windows.Storage.Pickers.FolderPicker();
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
        picker.FileTypeFilter.Add("*");

        var folder = await picker.PickSingleFolderAsync();
        if (folder is null)
            return;

        Song? first = null;
        foreach (var ext in AudioExtensions)
        {
            foreach (var file in System.IO.Directory.EnumerateFiles(folder.Path, $"*{ext}", System.IO.SearchOption.AllDirectories))
            {
                var song = await _libraryService.AddSongAsync(file);
                if (song is not null)
                {
                    _queueService.AddToQueue(song);
                    first ??= song;
                }
            }
        }

        if (first is not null && _viewModel.PlaybackState == PlaybackState.Stopped)
            await _viewModel.PlaySongAsync(first);
    }

    // ---------------------------------------------------------------
    // EQ
    // ---------------------------------------------------------------

    private void OnEqBandChanged(int band, double value)
    {
        if (_initializing)
            return;

        var settings = _settingsService.Settings;
        switch (band)
        {
            case 0: settings.EqB0 = value; break;
            case 1: settings.EqB1 = value; break;
            case 2: settings.EqB2 = value; break;
            case 3: settings.EqB3 = value; break;
            case 4: settings.EqB4 = value; break;
            case 5: settings.EqB5 = value; break;
            case 6: settings.EqB6 = value; break;
            case 7: settings.EqB7 = value; break;
            case 8: settings.EqB8 = value; break;
            case 9: settings.EqB9 = value; break;
        }
        ApplyEqToEngine();
        UpdateEqGraph();
        _ = _settingsService.SaveAsync();
    }

    private void ApplyEqToEngine()
    {
        var settings = _settingsService.Settings;
        var values = new[]
        {
            settings.EqB0, settings.EqB1, settings.EqB2, settings.EqB3, settings.EqB4,
            settings.EqB5, settings.EqB6, settings.EqB7, settings.EqB8, settings.EqB9,
        };

        if (settings.EqEnabled)
        {
            for (var i = 0; i < 10; i++)
                _playbackService.SetEqBand(i, (float)values[i]);
        }
        else
        {
            for (var i = 0; i < 10; i++)
                _playbackService.SetEqBand(i, 0f);
        }
    }

    private void ApplyPreset(string name)
    {
        var preset = EqPresets.FirstOrDefault(p => p.Name == name);
        if (preset.Gains is null || _eqSliders[0] is null)
            return;
        for (var i = 0; i < 10; i++)
            _eqSliders[i].Value = preset.Gains[i];
        _settingsService.Settings.EqPreset = name;
        _ = _settingsService.SaveAsync();
    }

    private void ApplyAutoPreset()
    {
        var genre = _viewModel.CurrentSong?.Genre ?? "";
        var match = EqPresets.FirstOrDefault(p =>
            !string.IsNullOrEmpty(genre) &&
            genre.Contains(p.Name, StringComparison.OrdinalIgnoreCase));
        ApplyPreset(match.Name ?? "Flat");
    }

    private void UpdateEqGraph()
    {
        EqGraphCanvas.Children.Clear();
        var settings = _settingsService.Settings;
        var values = new[]
        {
            settings.EqB0, settings.EqB1, settings.EqB2, settings.EqB3, settings.EqB4,
            settings.EqB5, settings.EqB6, settings.EqB7, settings.EqB8, settings.EqB9,
        };

        var w = EqGraphCanvas.ActualWidth > 0 ? EqGraphCanvas.ActualWidth : 384;
        var h = EqGraphCanvas.ActualHeight > 0 ? EqGraphCanvas.ActualHeight : 56;

        // grid lines
        for (var gx = 0; gx <= 4; gx++)
        {
            var line = new Rectangle
            {
                Width = 1,
                Height = h,
                Fill = new SolidColorBrush(Windows.UI.Color.FromArgb(40, 0x2B, 0xE2, 0x2B)),
            };
            EqGraphCanvas.Children.Add(line);
            Canvas.SetLeft(line, gx * (w - 1) / 4);
        }

        var polyline = new Polyline
        {
            Stroke = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 0x2B, 0xE2, 0x2B)),
            StrokeThickness = 2,
            StrokeLineJoin = PenLineJoin.Round,
        };
        for (var i = 0; i < 10; i++)
        {
            var x = 4 + i * ((w - 8) / 9.0);
            var y = (h / 2) - (values[i] / 12.0) * ((h / 2) - 3);
            polyline.Points.Add(new Windows.Foundation.Point(x, y));
        }
        EqGraphCanvas.Children.Add(polyline);
    }

    private void BtnEqOn_Click(object sender, RoutedEventArgs e)
    {
        _settingsService.Settings.EqEnabled = BtnEqOn.IsChecked == true;
        ApplyEqToEngine();
        _ = _settingsService.SaveAsync();
    }

    private void BtnEqAuto_Click(object sender, RoutedEventArgs e)
    {
        if (BtnEqAuto.IsChecked == true)
            ApplyAutoPreset();
    }

    // ---------------------------------------------------------------
    // Transport + toggles
    // ---------------------------------------------------------------

    private async void BtnPlay_Click(object sender, RoutedEventArgs e) => await _viewModel.PlayPauseCommand.ExecuteAsync(null);
    private async void BtnPause_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel.PlaybackState is PlaybackState.Playing or PlaybackState.Paused)
            await _viewModel.PlayPauseCommand.ExecuteAsync(null);
    }
    private async void BtnPrev_Click(object sender, RoutedEventArgs e) => await _viewModel.PreviousTrackCommand.ExecuteAsync(null);
    private async void BtnNext_Click(object sender, RoutedEventArgs e) => await _viewModel.NextTrackCommand.ExecuteAsync(null);
    private async void MenuPlayPause_Click(object sender, RoutedEventArgs e) => await _viewModel.PlayPauseCommand.ExecuteAsync(null);
    private async void MenuStop_Click(object sender, RoutedEventArgs e) => await _viewModel.StopPlaybackCommand.ExecuteAsync(null);
    private void BtnStop_Click(object sender, RoutedEventArgs e) => _ = _viewModel.StopPlaybackCommand.ExecuteAsync(null);
    private void MenuExit_Click(object sender, RoutedEventArgs e) => Close();
    private async void MenuAbout_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new ContentDialog
        {
            Title = "New Winamp Classic",
            Content = "New Winamp Classic 1.0\n\nA modern retro WinUI 3 music player for Windows.\nIt really whips the llama's ass!",
            CloseButtonText = "OK",
            XamlRoot = RootGrid.XamlRoot,
        };
        await dialog.ShowAsync();
    }

    private void BtnEject_Click(object sender, RoutedEventArgs e) => MenuOpenFiles_Click(sender, e);
    private void ClutterO_Click(object sender, RoutedEventArgs e) => MenuOpenFiles_Click(sender, e);
    private void ClutterI_Click(object sender, RoutedEventArgs e) => SetPanelVisible(MlWin, MlWin.Visibility != Visibility.Visible);

    private void ClutterA_Click(object sender, RoutedEventArgs e)
    {
        if (_appWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsAlwaysOnTop = !presenter.IsAlwaysOnTop;
            ToolTipService.SetToolTip(ClutterA, presenter.IsAlwaysOnTop ? "Always on top: ON" : "Always on top: OFF");
        }
    }

    private void ClutterD_Click(object sender, RoutedEventArgs e)
    {
        _doubleSize = !_doubleSize;
        var scale = _doubleSize ? 1.5 : 1.0;
        RootGrid.RenderTransform = new ScaleTransform { ScaleX = scale, ScaleY = scale };
        ResizeWindow();
    }

    private void ClutterV_Click(object sender, RoutedEventArgs e)
    {
        Spectrum.Visibility = Spectrum.Visibility == Visibility.Visible
            ? Visibility.Collapsed
            : Visibility.Visible;
    }

    private void TimeDisplay_Click(object sender, RoutedEventArgs e)
    {
        _showRemaining = !_showRemaining;
        TimeDisplay.Text = FormatDisplayTime();
    }

    private void BtnShuffle_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.ToggleShuffleCommand.Execute(null);
        BtnShuffle.IsChecked = _viewModel.ShuffleEnabled;
    }

    private void BtnRepeat_Click(object sender, RoutedEventArgs e)
    {
        _queueService.RepeatMode = BtnRepeat.IsChecked == true ? RepeatMode.All : RepeatMode.None;
        _viewModel.RepeatMode = _queueService.RepeatMode;
    }

    private void BtnMinimize_Click(object sender, RoutedEventArgs e)
        => ShowWindow(WindowNative.GetWindowHandle(this), 6 /* SW_MINIMIZE */);

    private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();

    private void MenuToggleEq_Click(object sender, RoutedEventArgs e) => BtnEqToggle_Click(sender, e);
    private void MenuTogglePl_Click(object sender, RoutedEventArgs e) => BtnPlToggle_Click(sender, e);
    private void MenuToggleMl_Click(object sender, RoutedEventArgs e) => BtnMlToggle_Click(sender, e);
    private void MenuAlwaysOnTop_Click(object sender, RoutedEventArgs e) => ClutterA_Click(sender, e);
    private void MenuStop_Click_Wire(object sender, RoutedEventArgs e) => MenuStop_Click(sender, e);

    private void BtnEqToggle_Click(object sender, RoutedEventArgs e) => SetPanelVisible(EqWin, EqWin.Visibility != Visibility.Visible);
    private void BtnPlToggle_Click(object sender, RoutedEventArgs e) => SetPanelVisible(PlWin, PlWin.Visibility != Visibility.Visible);

    private void BtnMlToggle_Click(object sender, RoutedEventArgs e)
    {
        var show = MlWin.Visibility != Visibility.Visible;
        SetPanelVisible(MlWin, show);
        if (show && !_mlInitialized)
        {
            _mlInitialized = true;
            if (ContentFrame.Content is null)
                ContentFrame.Navigate(typeof(Views.HomePage));
            NavHome.IsChecked = true;
        }
    }

    private void SetPanelVisible(FrameworkElement panel, bool visible)
    {
        panel.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        ResizeWindow();
    }

    private void Nav_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not ToggleButton button)
            return;

        var pageType = button.Tag?.ToString() switch
        {
            "Home" => typeof(Views.HomePage),
            "Library" => typeof(Views.LibraryPage),
            "Artists" => typeof(Views.ArtistsPage),
            "Albums" => typeof(Views.AlbumsPage),
            "Playlists" => typeof(Views.PlaylistsPage),
            "Favorites" => typeof(Views.FavoritesPage),
            "Recent" => typeof(Views.RecentlyPlayedPage),
            "Downloads" => typeof(Views.DownloadsPage),
            "Search" => typeof(Views.SearchPage),
            "Settings" => typeof(Views.SettingsPage),
            _ => typeof(Views.HomePage),
        };

        foreach (var child in GetAllNavButtons())
            child.IsChecked = ReferenceEquals(child, button);

        ContentFrame.Navigate(pageType);
    }

    private IEnumerable<ToggleButton> GetAllNavButtons()
    {
        yield return NavHome;
        yield return NavLibrary;
        yield return NavArtists;
        yield return NavAlbums;
        yield return NavPlaylists;
        yield return NavFavorites;
        yield return NavRecent;
        yield return NavSearch;
        yield return NavSettings;
    }

    // ---------------------------------------------------------------
    // Shutdown
    // ---------------------------------------------------------------

    private async void AppWindow_Closing(AppWindow sender, AppWindowClosingEventArgs args)
    {
        if (_isClosing)
            return;

        args.Cancel = true;

        _viewModel.PropertyChanged -= OnViewModelPropertyChanged;

        _settingsService.Settings.Balance = BalanceSlider.Value / 50.0;

        try
        {
            await _viewModel.StopAndSaveAsync();
        }
        catch (Exception)
        {
        }
        finally
        {
            _isClosing = true;
            Close();
        }
    }
}
