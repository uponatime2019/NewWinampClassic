namespace NewWinampClassic.Services;

public interface INavigationService
{
    void SetFrame(Microsoft.UI.Xaml.Controls.Frame frame);
    void NavigateTo(Type pageType, object? parameter = null);
    void NavigateTo(string pageKey, object? parameter = null);
    bool CanGoBack { get; }
    bool CanGoForward { get; }
    void GoBack();
    void GoForward();

    event EventHandler<Microsoft.UI.Xaml.Navigation.NavigationEventArgs>? Navigated;
}
