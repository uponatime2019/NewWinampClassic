using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Navigation;
using NewWinampClassic.Models;
using NewWinampClassic.ViewModels;

namespace NewWinampClassic.Views;

public sealed partial class AlbumsPage : Page
{
    public AlbumsViewModel ViewModel { get; }

    public AlbumsPage()
    {
        ViewModel = App.Services.GetRequiredService<AlbumsViewModel>();
        InitializeComponent();
        BuildSongTemplate();

        AlbumsGridView.ItemClick += async (_, args) =>
        {
            if (args.ClickedItem is Album album)
            {
                await ViewModel.OpenAlbumCommand.ExecuteAsync(album);
                ShowAlbumDetail();
            }
        };

        AlbumSongsListView.ItemClick += async (_, args) =>
        {
            if (args.ClickedItem is Song song)
            {
                AlbumSongsListView.SelectedItem = song;
                await ViewModel.PlaySongCommand.ExecuteAsync(song);
            }
        };
    }

    private void BuildSongTemplate()
    {
        AlbumSongsListView.ItemTemplate = (DataTemplate)XamlReader.Load("""
            <DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>
                <Grid Height="22" ColumnSpacing="4" Margin="4,0,4,0">
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="20" />
                        <ColumnDefinition Width="*" />
                        <ColumnDefinition Width="46" />
                        <ColumnDefinition Width="20" />
                    </Grid.ColumnDefinitions>
                    <TextBlock Grid.Column="0" Text="{Binding TrackNumber}" FontFamily="Consolas" FontSize="10" Foreground="#008800" VerticalAlignment="Center" />
                    <TextBlock Grid.Column="1" FontFamily="Consolas" FontSize="11" Foreground="#00E800" TextTrimming="CharacterEllipsis" VerticalAlignment="Center">
                        <Run Text="{Binding DisplayTitle}" />
                        <Run Text=" - " Foreground="#008800" />
                        <Run Text="{Binding DisplayArtist}" Foreground="#00B800" />
                    </TextBlock>
                    <TextBlock Grid.Column="2" Text="{Binding DurationText}" FontFamily="Consolas" FontSize="10" Foreground="#00B800" TextAlignment="Right" VerticalAlignment="Center" />
                    <Button Grid.Column="3" Command="{Binding ToggleFavoriteCommand}" Background="Transparent" BorderThickness="0" Padding="0" Width="18" Height="18" HorizontalAlignment="Right" VerticalAlignment="Center">
                        <FontIcon Glyph="{Binding IsFavorite, Converter={StaticResource FavIconConv}}" Foreground="{Binding IsFavorite, Converter={StaticResource FavColorConv}}" FontSize="10" />
                    </Button>
                </Grid>
            </DataTemplate>
            """);
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        await ViewModel.InitializeAsync();
        AlbumsGridView.ItemsSource = ViewModel.Albums;
    }

    private void ShowAlbumDetail()
    {
        AlbumsScrollViewer.Visibility = Visibility.Collapsed;
        AlbumDetailPanel.Visibility = Visibility.Visible;

        var album = ViewModel.SelectedAlbum;
        if (album is not null)
        {
            DetailTitle.Text = album.Name;
            DetailSubtitle.Text = $"{album.Artist} · {album.TrackCount} tracks";
        }

        AlbumSongsListView.ItemsSource = ViewModel.AlbumSongs;
    }

    private void Back_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.GoBackCommand.Execute(null);
        AlbumDetailPanel.Visibility = Visibility.Collapsed;
        AlbumsScrollViewer.Visibility = Visibility.Visible;
    }
}
