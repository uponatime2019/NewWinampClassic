using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Navigation;
using NewWinampClassic.Models;
using NewWinampClassic.ViewModels;

namespace NewWinampClassic.Views;

public sealed partial class LibraryPage : Page
{
    public LibraryViewModel ViewModel { get; }

    public LibraryPage()
    {
        ViewModel = App.Services.GetRequiredService<LibraryViewModel>();
        InitializeComponent();
        BuildItemTemplate();
        SongListView.ItemClick += async (_, args) =>
        {
            if (args.ClickedItem is Song song)
            {
                SongListView.SelectedItem = song;
                await ViewModel.PlaySongCommand.ExecuteAsync(song);
            }
        };
    }

    private void BuildItemTemplate()
    {
        SongListView.ItemTemplate = (DataTemplate)XamlReader.Load("""
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
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        await ViewModel.InitializeAsync();
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (SongListView is not null)
            SongListView.ItemsSource = ViewModel.Songs;
        if (FooterText is not null)
            FooterText.Text = $"{ViewModel.TotalCount} songs";
    }

    private async void Refresh_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.RefreshCommand.ExecuteAsync(null);
        UpdateUI();
    }

    private async void SearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        ViewModel.SearchQuery = args.QueryText;
        await ViewModel.SearchCommand.ExecuteAsync(null);
        UpdateUI();
    }

    private async void SortCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (SortCombo.SelectedIndex >= 0 && ViewModel is not null)
        {
            ViewModel.SortField = SortCombo.SelectedIndex switch
            {
                0 => SortField.Title,
                1 => SortField.Artist,
                2 => SortField.Album,
                3 => SortField.Duration,
                4 => SortField.DateAdded,
                5 => SortField.PlayCount,
                _ => SortField.Title
            };
            await ViewModel.RefreshCommand.ExecuteAsync(null);
            UpdateUI();
        }
    }

    private async void SortDirection_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.SortDescending = SortDirectionBtn.IsChecked ?? false;
        await ViewModel.RefreshCommand.ExecuteAsync(null);
        UpdateUI();
    }
}
