using CommunityToolkit.Mvvm.ComponentModel;

namespace NewWinampClassic.Models;

public partial class Genre : ObservableObject
{
    [ObservableProperty] private int _id;
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private int _trackCount;
}
