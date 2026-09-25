using Microsoft.Extensions.Logging;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace NewWinampClassic.Services;

public sealed class ThemeService : IThemeService
{
    private readonly ILogger<ThemeService> _logger;
    private readonly Window _window;
    private string _currentTheme = "System";

    public event EventHandler? ThemeChanged;

    public ThemeService(ILogger<ThemeService> logger, Window window)
    {
        _logger = logger;
        _window = window;
    }

    public void SetTheme(string theme)
    {
        _logger.LogInformation("Setting theme to: {Theme}", theme);

        _currentTheme = theme;

        switch (theme)
        {
            case "Light":
                Application.Current.RequestedTheme = ApplicationTheme.Light;
                break;
            case "Dark":
                Application.Current.RequestedTheme = ApplicationTheme.Dark;
                break;
            case "System":
            default:
                _currentTheme = "System";
                break;
        }

        ApplyBackdrop();
        ThemeChanged?.Invoke(this, EventArgs.Empty);
        _logger.LogInformation("Theme applied: {Theme}", _currentTheme);
    }

    public string GetCurrentTheme()
    {
        return _currentTheme;
    }

    private void ApplyBackdrop()
    {
        try
        {
            _window.SystemBackdrop = new MicaBackdrop();
            _logger.LogDebug("Mica backdrop applied.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to apply Mica backdrop.");
        }
    }
}
