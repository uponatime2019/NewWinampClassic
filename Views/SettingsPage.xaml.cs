using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using NewWinampClassic.ViewModels;

namespace NewWinampClassic.Views;

public sealed partial class SettingsPage : Page
{
    public SettingsViewModel ViewModel { get; }

    public SettingsPage()
    {
        ViewModel = App.Services.GetRequiredService<SettingsViewModel>();
        InitializeComponent();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        await ViewModel.InitializeAsync();

        if (AppNameText is not null)
        {
            AppNameText.Text = App.AppDisplayName;
        }

        FoldersListView.ItemsSource = ViewModel.LibraryPaths;
        AutoScanToggle.IsOn = ViewModel.AutoScanOnStartup;

        var themeIndex = ViewModel.SelectedTheme switch { "Light" => 1, "Dark" => 2, _ => 0 };
        ThemeCombo.SelectedIndex = themeIndex;
    }

    private void ThemeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ViewModel is null) return;
        ViewModel.SelectedTheme = ThemeCombo.SelectedIndex switch { 1 => "Light", 2 => "Dark", _ => "System" };
    }



    private void AutoScanToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (ViewModel is not null) ViewModel.AutoScanOnStartup = AutoScanToggle.IsOn;
    }

    private async void AddFolder_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.AddLibraryPathCommand.ExecuteAsync(null);
    }

    private void RemoveFolder_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is string path)
        {
            ViewModel.RemoveLibraryPathCommand.Execute(path);
        }
    }

    private async void Scan_Click(object sender, RoutedEventArgs e)
    {
        ScanProgress.Visibility = Visibility.Visible;
        ScanStatusText.Visibility = Visibility.Visible;
        ScanButton.IsEnabled = false;

        ViewModel.PropertyChanged += (s, args) =>
        {
            if (args.PropertyName == nameof(SettingsViewModel.ScanningStatus))
            {
                DispatcherQueue.TryEnqueue(() => ScanStatusText.Text = ViewModel.ScanningStatus);
            }
            if (args.PropertyName == nameof(SettingsViewModel.IsScanning))
            {
                DispatcherQueue.TryEnqueue(() =>
                {
                    ScanButton.IsEnabled = !ViewModel.IsScanning;
                    if (!ViewModel.IsScanning) ScanProgress.Visibility = Visibility.Collapsed;
                });
            }
        };

        await ViewModel.ScanLibraryCommand.ExecuteAsync(null);
    }
}
