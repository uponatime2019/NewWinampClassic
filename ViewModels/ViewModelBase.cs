using CommunityToolkit.Mvvm.ComponentModel;

namespace NewWinampClassic.ViewModels;

public abstract partial class ViewModelBase : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotBusy))]
    private bool _isBusy;

    [ObservableProperty]
    private string _title = string.Empty;

    public bool IsNotBusy => !IsBusy;

    public virtual async Task InitializeAsync(object? parameter = null) => await Task.CompletedTask;

    public virtual Task OnNavigatedFromAsync() => Task.CompletedTask;
}
