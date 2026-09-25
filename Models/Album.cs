using CommunityToolkit.Mvvm.ComponentModel;

namespace NewWinampClassic.Models;

public partial class Album : ObservableObject
{
    [ObservableProperty] private int _id;
    [ObservableProperty] private string _name = "Unknown Album";
    [ObservableProperty] private string _artist = "Unknown Artist";
    [ObservableProperty] private int _artistId = -1;
    [ObservableProperty] private string _artworkPath = string.Empty;
    [ObservableProperty] private int _year;
    [ObservableProperty] private int _trackCount;
    [ObservableProperty] private TimeSpan _totalDuration;
    [ObservableProperty] private DateTime _dateAdded;

    public string TotalDurationText => $"{(int)TotalDuration.TotalHours}:{TotalDuration.Minutes:D2}:{TotalDuration.Seconds:D2}";
}
