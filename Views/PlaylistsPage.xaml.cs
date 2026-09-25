using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using NewWinampClassic.Models;
using NewWinampClassic.ViewModels;

namespace NewWinampClassic.Views;

public sealed partial class PlaylistsPage : Page
{
    public PlaylistsViewModel ViewModel { get; }

    public PlaylistsPage()
    {
        ViewModel = App.Services.GetRequiredService<PlaylistsViewModel>();
        InitializeComponent();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        await ViewModel.InitializeAsync();
        PlaylistListView.ItemsSource = ViewModel.Playlists;
        UpdateContentVisibility();
    }

    private async void PlaylistListView_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is Playlist playlist)
        {
            await ViewModel.SelectPlaylistCommand.ExecuteAsync(playlist);
            PlaylistListView.SelectedItem = playlist;
            UpdateContentVisibility();
        }
    }

    private void UpdateContentVisibility()
    {
        var hasSongs = ViewModel.SelectedPlaylistSongs.Count > 0;
        EmptyText.Visibility = ViewModel.IsPlaylistSelected && !hasSongs ? Visibility.Visible : Visibility.Collapsed;
        SongListView.Visibility = ViewModel.IsPlaylistSelected ? Visibility.Visible : Visibility.Collapsed;
        PlayAllBtn.Visibility = hasSongs ? Visibility.Visible : Visibility.Collapsed;
        PlaylistTitleText.Text = ViewModel.SelectedPlaylistName;

        SongListView.ItemsSource = null;
        SongListView.ItemsSource = ViewModel.SelectedPlaylistSongs;
    }

    private async void SongListView_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is Song song)
            await ViewModel.PlaySongCommand.ExecuteAsync(song);
    }

    private async void PlayAll_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.PlayAllCommand.ExecuteAsync(null);
    }

    private async void CreatePlaylist_Click(object sender, RoutedEventArgs e)
    {
        var result = await CreateDialog.ShowAsync();
        if (result == ContentDialogResult.Primary && !string.IsNullOrWhiteSpace(PlaylistNameBox.Text))
        {
            ViewModel.NewPlaylistName = PlaylistNameBox.Text;
            await ViewModel.CreatePlaylistCommand.ExecuteAsync(null);
            PlaylistNameBox.Text = string.Empty;
            PlaylistListView.ItemsSource = null;
            PlaylistListView.ItemsSource = ViewModel.Playlists;
        }
    }
}
