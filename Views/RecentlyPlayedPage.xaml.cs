using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using NewWinampClassic.ViewModels;

namespace NewWinampClassic.Views;

public sealed partial class RecentlyPlayedPage : Page
{
    public RecentlyPlayedViewModel ViewModel { get; }

    public RecentlyPlayedPage()
    {
        ViewModel = App.Services.GetRequiredService<RecentlyPlayedViewModel>();
        InitializeComponent();
        BuildItemTemplate();

        RecentListView.ItemClick += async (_, args) =>
        {
            if (args.ClickedItem is Models.Song song)
                await ViewModel.PlaySongCommand.ExecuteAsync(song);
        };
    }

    private void BuildItemTemplate()
    {
        RecentListView.ItemTemplate = (DataTemplate)Microsoft.UI.Xaml.Markup.XamlReader.Load("""
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
        if (RecentListView is not null)
            RecentListView.ItemsSource = ViewModel.RecentSongs;
    }

    private async void PlayAll_Click(object sender, RoutedEventArgs e) => await ViewModel.PlayAllCommand.ExecuteAsync(null);
}
