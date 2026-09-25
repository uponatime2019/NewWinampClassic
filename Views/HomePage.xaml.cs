using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using NewWinampClassic.Services;
using NewWinampClassic.ViewModels;

namespace NewWinampClassic.Views;

public sealed partial class HomePage : Page
{
    public HomeViewModel ViewModel { get; }
    private readonly INavigationService _navigationService;
    private readonly SettingsViewModel _settingsViewModel;

    public HomePage()
    {
        ViewModel = App.Services.GetRequiredService<HomeViewModel>();
        _navigationService = App.Services.GetRequiredService<INavigationService>();
        _settingsViewModel = App.Services.GetRequiredService<SettingsViewModel>();
        InitializeComponent();
        BuildTemplates();

        RecentListView.ItemClick += async (_, args) =>
        {
            if (args.ClickedItem is Models.Song song)
                await ViewModel.PlaySongCommand.ExecuteAsync(song);
        };

        MostPlayedGridView.ItemClick += async (_, args) =>
        {
            if (args.ClickedItem is Models.Song song)
                await ViewModel.PlaySongCommand.ExecuteAsync(song);
        };

        SuggestedListView.ItemClick += async (_, args) =>
        {
            if (args.ClickedItem is Models.Song song)
            {
                SuggestedListView.SelectedItem = song;
                await ViewModel.PlaySongCommand.ExecuteAsync(song);
            }
        };
    }

    private void BuildTemplates()
    {
        var songListTemplate = (DataTemplate)Microsoft.UI.Xaml.Markup.XamlReader.Load("""
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

        RecentListView.ItemTemplate = songListTemplate;
        MostPlayedGridView.ItemTemplate = songListTemplate;
        SuggestedListView.ItemTemplate = songListTemplate;
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        await ViewModel.InitializeAsync();

        if (WelcomeText is not null)
        {
            WelcomeText.Text = "WINAMP MEDIA LIBRARY";
        }

        if (SetupPanel is not null)
        {
            SetupPanel.Visibility = ViewModel.TotalSongs < 10
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        if (SongsCountText is not null) SongsCountText.Text = ViewModel.TotalSongs.ToString();
        if (AlbumsCountText is not null) AlbumsCountText.Text = ViewModel.TotalAlbums.ToString();
        if (ArtistsCountText is not null) ArtistsCountText.Text = ViewModel.TotalArtists.ToString();

        if (RecentListView is not null) RecentListView.ItemsSource = ViewModel.RecentSongs;
        if (MostPlayedGridView is not null) MostPlayedGridView.ItemsSource = ViewModel.MostPlayed;

        if (SuggestedSection is not null)
            SuggestedSection.Visibility = ViewModel.SuggestedSongs.Count > 0
                ? Visibility.Visible
                : Visibility.Collapsed;
        if (SuggestedListView is not null) SuggestedListView.ItemsSource = ViewModel.SuggestedSongs;
    }

    private void SongsCard_Tapped(object sender, RoutedEventArgs e) => _navigationService.NavigateTo("Library");
    private void AlbumsCard_Tapped(object sender, RoutedEventArgs e) => _navigationService.NavigateTo("Albums");
    private void ArtistsCard_Tapped(object sender, RoutedEventArgs e) => _navigationService.NavigateTo("Artists");

    private void GoToSettings_Click(object sender, RoutedEventArgs e)
    {
        _navigationService.NavigateTo("Settings");
    }

    private void HomeSearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        if (!string.IsNullOrWhiteSpace(args.QueryText))
        {
            _navigationService.NavigateTo("Search", args.QueryText);
        }
    }
}
