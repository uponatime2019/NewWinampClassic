using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using NewWinampClassic.Models;
using NewWinampClassic.ViewModels;

namespace NewWinampClassic.Views;

public sealed partial class SearchPage : Page
{
    public SearchViewModel SearchViewModel { get; }
    public DownloadsViewModel DownloadsViewModel { get; }

    public SearchPage()
    {
        SearchViewModel = App.Services.GetRequiredService<SearchViewModel>();
        DownloadsViewModel = App.Services.GetRequiredService<DownloadsViewModel>();
        InitializeComponent();
        BuildTemplates();

        ResultsListView.ItemClick += async (_, e) =>
        {
            if (e.ClickedItem is Song song)
                await SearchViewModel.PlaySongCommand.ExecuteAsync(song);
        };

        ArtistsListView.ItemClick += async (_, e) =>
        {
            if (e.ClickedItem is Artist artist)
                await SearchViewModel.PlayArtistCommand.ExecuteAsync(artist);
        };

        DownloadsViewModel.PropertyChanged += (_, args) =>
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                switch (args.PropertyName)
                {
                    case nameof(DownloadsViewModel.StatusMessage):
                        StatusText.Text = DownloadsViewModel.StatusMessage;
                        StatusRow.Visibility = string.IsNullOrEmpty(DownloadsViewModel.StatusMessage)
                            ? Visibility.Collapsed
                            : Visibility.Visible;
                        break;
                    case nameof(DownloadsViewModel.DownloadProgress):
                        DownloadProgressBar.Value = DownloadsViewModel.DownloadProgress;
                        DownloadProgressBar.Visibility = DownloadsViewModel.IsDownloading
                            ? Visibility.Visible
                            : Visibility.Collapsed;
                        break;
                }
            });
        };
    }

    private void BuildTemplates()
    {
        var artistTemplate = (DataTemplate)Microsoft.UI.Xaml.Markup.XamlReader.Load("""
            <DataTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation">
                <Grid Height="22" Margin="4,0,4,0">
                    <TextBlock Text="{Binding Name}" FontFamily="Consolas" FontSize="11" Foreground="#00E800" TextTrimming="CharacterEllipsis" VerticalAlignment="Center" />
                </Grid>
            </DataTemplate>
            """);

        var songTemplate = (DataTemplate)Microsoft.UI.Xaml.Markup.XamlReader.Load("""
            <DataTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation">
                <Grid Height="22" ColumnSpacing="4" Margin="4,0,4,0">
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="*" />
                        <ColumnDefinition Width="46" />
                        <ColumnDefinition Width="20" />
                    </Grid.ColumnDefinitions>
                    <TextBlock Grid.Column="0" FontFamily="Consolas" FontSize="11" Foreground="#00E800" TextTrimming="CharacterEllipsis" VerticalAlignment="Center">
                        <Run Text="{Binding DisplayTitle}" />
                        <Run Text=" - " Foreground="#008800" />
                        <Run Text="{Binding DisplayArtist}" Foreground="#00B800" />
                    </TextBlock>
                    <TextBlock Grid.Column="1" Text="{Binding DurationText}" FontFamily="Consolas" FontSize="10" Foreground="#00B800" TextAlignment="Right" VerticalAlignment="Center" />
                    <Button Grid.Column="2" Command="{Binding ToggleFavoriteCommand}" Background="Transparent" BorderThickness="0" Padding="0" Width="18" Height="18" HorizontalAlignment="Right" VerticalAlignment="Center">
                        <FontIcon Glyph="{Binding IsFavorite, Converter={StaticResource FavIconConv}}" Foreground="{Binding IsFavorite, Converter={StaticResource FavColorConv}}" FontSize="10" />
                    </Button>
                </Grid>
            </DataTemplate>
            """);

        var onlineTrackTemplate = (DataTemplate)Microsoft.UI.Xaml.Markup.XamlReader.Load("""
            <DataTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation">
                <Grid Height="22" ColumnSpacing="4" Margin="4,0,4,0">
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="*" />
                        <ColumnDefinition Width="Auto" />
                    </Grid.ColumnDefinitions>
                    <TextBlock Grid.Column="0" FontFamily="Consolas" FontSize="11" Foreground="#00E800" TextTrimming="CharacterEllipsis" VerticalAlignment="Center">
                        <Run Text="{Binding Title}" />
                        <Run Text=" - " Foreground="#008800" />
                        <Run Text="{Binding DisplayArtist}" Foreground="#00B800" />
                    </TextBlock>
                    <StackPanel Grid.Column="1" Orientation="Horizontal" Spacing="4" VerticalAlignment="Center">
                        <Button Click="DownloadOnlineTrack_Click" Background="Transparent" BorderThickness="0" Padding="0" Width="18" Height="18" ToolTipService.ToolTip="Download to library">
                            <FontIcon Glyph="&#xE896;" FontSize="10" Foreground="#00E800" />
                        </Button>
                    </StackPanel>
                </Grid>
            </DataTemplate>
            """);

        if (ArtistsListView is not null) ArtistsListView.ItemTemplate = artistTemplate;
        if (ResultsListView is not null) ResultsListView.ItemTemplate = songTemplate;
        if (OnlineResultsListView is not null) OnlineResultsListView.ItemTemplate = onlineTrackTemplate;
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (e.Parameter is string query && !string.IsNullOrWhiteSpace(query))
        {
            SearchBox.Text = query;
            await ExecuteSearchAsync(query);
        }
    }

    private void LocalMode_Click(object sender, RoutedEventArgs e)
    {
        BtnLocalMode.IsChecked = true;
        BtnOnlineMode.IsChecked = false;
        LocalResultsPanel.Visibility = Visibility.Visible;
        OnlineResultsPanel.Visibility = Visibility.Collapsed;
        SearchBox.PlaceholderText = "Search local songs, albums, artists...";
        if (!string.IsNullOrWhiteSpace(SearchBox.Text))
        {
            _ = ExecuteSearchAsync(SearchBox.Text);
        }
    }

    private void OnlineMode_Click(object sender, RoutedEventArgs e)
    {
        BtnLocalMode.IsChecked = false;
        BtnOnlineMode.IsChecked = true;
        LocalResultsPanel.Visibility = Visibility.Collapsed;
        OnlineResultsPanel.Visibility = Visibility.Visible;
        SearchBox.PlaceholderText = "Search online SoundCloud tracks...";
        if (!string.IsNullOrWhiteSpace(SearchBox.Text))
        {
            _ = ExecuteSearchAsync(SearchBox.Text);
        }
    }

    private async Task ExecuteSearchAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return;

        if (BtnLocalMode.IsChecked == true)
        {
            SearchViewModel.SearchQuery = query;
            await SearchViewModel.SearchCommand.ExecuteAsync(null);

            if (ResultsListView is not null) ResultsListView.ItemsSource = SearchViewModel.Results;
            if (ArtistsListView is not null) ArtistsListView.ItemsSource = SearchViewModel.ArtistResults;

            if (ResultsHeader is not null)
                ResultsHeader.Visibility = SearchViewModel.HasSearched ? Visibility.Visible : Visibility.Collapsed;
            if (ArtistsSection is not null)
                ArtistsSection.Visibility = SearchViewModel.ArtistResults.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        }
        else
        {
            DownloadsViewModel.SearchQuery = query;
            OnlineResultsListView.ItemsSource = null;
            await DownloadsViewModel.SearchCommand.ExecuteAsync(null);
            OnlineResultsListView.ItemsSource = DownloadsViewModel.SearchResults;
        }
    }

    private async void SearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        await ExecuteSearchAsync(args.QueryText);
    }

    private async void OnlineResultsListView_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is SoundCloudTrack track)
        {
            await DownloadsViewModel.PlayTrackCommand.ExecuteAsync(track);
        }
    }

    private async void DownloadOnlineTrack_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is SoundCloudTrack track)
        {
            await DownloadsViewModel.DownloadCommand.ExecuteAsync(track);
        }
    }
}
