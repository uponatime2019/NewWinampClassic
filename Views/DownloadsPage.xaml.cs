using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using NewWinampClassic.Models;
using NewWinampClassic.ViewModels;

namespace NewWinampClassic.Views;

public sealed partial class DownloadsPage : Page
{
    public DownloadsViewModel ViewModel { get; }

    public DownloadsPage()
    {
        ViewModel = App.Services.GetRequiredService<DownloadsViewModel>();
        InitializeComponent();

        ViewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        UpdateUI();
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            switch (e.PropertyName)
            {
                case nameof(DownloadsViewModel.StatusMessage):
                    StatusText.Text = ViewModel.StatusMessage;
                    break;
                case nameof(DownloadsViewModel.DownloadProgress):
                    DownloadProgressBar.Value = ViewModel.DownloadProgress;
                    DownloadProgressBar.Visibility = ViewModel.IsDownloading
                        ? Visibility.Visible
                        : Visibility.Collapsed;
                    break;
                case nameof(DownloadsViewModel.SearchResults):
                    UpdateResultsVisibility();
                    break;
            }
        });
    }

    private void UpdateUI()
    {
        StatusText.Text = ViewModel.StatusMessage;
        ResultsListView.ItemsSource = ViewModel.SearchResults;
        RebuildRecentSearchButtons();
        UpdateResultsVisibility();
    }

    private void RebuildRecentSearchButtons()
    {
        RecentSearchesPanel.Children.Clear();
        foreach (var query in ViewModel.RecentSearches)
        {
            var btn = new Button
            {
                Tag = query,
                Background = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["ControlFillColorDefaultBrush"],
                BorderThickness = new Thickness(0),
                CornerRadius = new CornerRadius(16),
                Padding = new Thickness(12, 6, 12, 6),
                FontSize = 13
            };
            btn.Click += RecentSearch_Click;
            var stack = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
            stack.Children.Add(new FontIcon { Glyph = "", FontSize = 12, Opacity = 0.6 });
            stack.Children.Add(new TextBlock { Text = query });
            btn.Content = stack;
            RecentSearchesPanel.Children.Add(btn);
        }
    }

    private void UpdateResultsVisibility()
    {
        var hasResults = ViewModel.SearchResults.Count > 0;
        RecentSearchesPanel.Visibility = hasResults ? Visibility.Collapsed : Visibility.Visible;
    }

    private async void SearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        if (!string.IsNullOrWhiteSpace(args.QueryText))
        {
            ViewModel.SearchQuery = args.QueryText;
            ResultsListView.ItemsSource = null;
            await ViewModel.SearchCommand.ExecuteAsync(null);
            ResultsListView.ItemsSource = ViewModel.SearchResults;
            RebuildRecentSearchButtons();
            UpdateResultsVisibility();
        }
    }

    private async void RecentSearch_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string query)
        {
            SearchBox.Text = query;
            ViewModel.SearchQuery = query;
            ResultsListView.ItemsSource = null;
            await ViewModel.SearchCommand.ExecuteAsync(null);
            ResultsListView.ItemsSource = ViewModel.SearchResults;
        }
    }

    private async void ResultsListView_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is SoundCloudTrack track)
        {
            await ViewModel.PlayTrackCommand.ExecuteAsync(track);
        }
    }

    private async void PlayButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is SoundCloudTrack track)
        {
            await ViewModel.PlayTrackCommand.ExecuteAsync(track);
        }
    }

    private async void DownloadButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is SoundCloudTrack track)
        {
            btn.IsEnabled = false;
            try
            {
                await ViewModel.DownloadCommand.ExecuteAsync(track);
            }
            finally
            {
                btn.IsEnabled = true;
            }
        }
    }
}
