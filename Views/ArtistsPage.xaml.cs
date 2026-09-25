using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Navigation;
using NewWinampClassic.ViewModels;

namespace NewWinampClassic.Views;

public sealed partial class ArtistsPage : Page
{
    public ArtistsViewModel ViewModel { get; }

    public ArtistsPage()
    {
        ViewModel = App.Services.GetRequiredService<ArtistsViewModel>();
        InitializeComponent();
        BuildItemTemplate();

        ArtistsGridView.ItemClick += async (_, args) =>
        {
            if (args.ClickedItem is Models.Artist artist)
                await ViewModel.OpenArtistCommand.ExecuteAsync(artist);
        };
    }

    private void BuildItemTemplate()
    {
        var template = (DataTemplate)XamlReader.Load("""
            <DataTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation">
                <Grid Height="24" ColumnSpacing="6" Margin="4,0,6,0">
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="*" />
                        <ColumnDefinition Width="Auto" />
                    </Grid.ColumnDefinitions>
                    <TextBlock Text="{Binding Name}" FontFamily="Consolas" FontSize="11" Foreground="#00E800" TextTrimming="CharacterEllipsis" VerticalAlignment="Center" />
                    <TextBlock Grid.Column="1" Text="{Binding TrackCountText}" FontFamily="Consolas" FontSize="10" Foreground="#00B800" VerticalAlignment="Center" />
                </Grid>
            </DataTemplate>
            """);
        ArtistsGridView.ItemTemplate = template;
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        await ViewModel.InitializeAsync();
        ArtistsGridView.ItemsSource = ViewModel.Artists;
    }
}
