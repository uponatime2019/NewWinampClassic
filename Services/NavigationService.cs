using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace NewWinampClassic.Services;

public sealed class NavigationService : INavigationService
{
    private readonly ILogger<NavigationService> _logger;
    private Frame? _frame;

    private static readonly Dictionary<string, Type> PageKeyToType = new(StringComparer.OrdinalIgnoreCase)
    {
        { "Home", typeof(NewWinampClassic.Views.HomePage) },
        { "Downloads", typeof(NewWinampClassic.Views.SearchPage) },
        { "Library", typeof(NewWinampClassic.Views.LibraryPage) },
        { "Albums", typeof(NewWinampClassic.Views.AlbumsPage) },
        { "Artists", typeof(NewWinampClassic.Views.ArtistsPage) },
        { "Browse", typeof(NewWinampClassic.Views.AlbumsPage) },
        { "Playlists", typeof(NewWinampClassic.Views.PlaylistsPage) },
        { "Favorites", typeof(NewWinampClassic.Views.FavoritesPage) },
        { "RecentlyPlayed", typeof(NewWinampClassic.Views.RecentlyPlayedPage) },
        { "Queue", typeof(NewWinampClassic.Views.QueuePage) },
        { "Search", typeof(NewWinampClassic.Views.SearchPage) },
        { "Settings", typeof(NewWinampClassic.Views.SettingsPage) },
    };

    public bool CanGoBack => _frame?.CanGoBack ?? false;
    public bool CanGoForward => _frame?.CanGoForward ?? false;

    public event EventHandler<NavigationEventArgs>? Navigated;

    public NavigationService(ILogger<NavigationService> logger)
    {
        _logger = logger;
    }

    public void SetFrame(Frame frame)
    {
        if (_frame is not null)
        {
            _frame.Navigated -= OnFrameNavigated;
        }

        _frame = frame;
        _frame.Navigated += OnFrameNavigated;
        _logger.LogInformation("Navigation frame set.");
    }

    public void NavigateTo(Type pageType, object? parameter = null)
    {
        if (_frame is null)
        {
            _logger.LogWarning("NavigateTo called but no frame is set.");
            return;
        }

        _logger.LogInformation("Navigating to {PageType}", pageType.Name);

        if (_frame.Content?.GetType() == pageType)
        {
            _logger.LogDebug("Already on {PageType}, skipping navigation.", pageType.Name);
            return;
        }

        _frame.Navigate(pageType, parameter);
    }

    public void NavigateTo(string pageKey, object? parameter = null)
    {
        if (PageKeyToType.TryGetValue(pageKey, out var pageType))
        {
            NavigateTo(pageType, parameter);
        }
        else
        {
            _logger.LogWarning("Unknown page key: {PageKey}", pageKey);
        }
    }

    public void GoBack()
    {
        if (_frame is null || !_frame.CanGoBack)
        {
            _logger.LogWarning("GoBack called but cannot go back.");
            return;
        }

        _logger.LogInformation("Navigating back.");
        _frame.GoBack();
    }

    public void GoForward()
    {
        if (_frame is null || !_frame.CanGoForward)
        {
            _logger.LogWarning("GoForward called but cannot go forward.");
            return;
        }

        _logger.LogInformation("Navigating forward.");
        _frame.GoForward();
    }

    private void OnFrameNavigated(object sender, NavigationEventArgs e)
    {
        _logger.LogInformation("Navigated to {PageType}", e.SourcePageType?.Name);
        Navigated?.Invoke(sender, e);
    }
}
