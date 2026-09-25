namespace NewWinampClassic.Services;

public interface IThemeService
{
    void SetTheme(string theme);
    string GetCurrentTheme();

    event EventHandler? ThemeChanged;
}
