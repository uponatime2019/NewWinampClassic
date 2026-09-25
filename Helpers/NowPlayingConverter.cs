using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;

namespace NewWinampClassic.Helpers;

public class NowPlayingConverter : IValueConverter
{
    private SolidColorBrush? _accentBrush;

    private SolidColorBrush AccentBrush => _accentBrush ??= new SolidColorBrush(
        (Windows.UI.Color)Application.Current.Resources["SystemAccentColor"]);

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is true)
            return AccentBrush;
        return DependencyProperty.UnsetValue;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}
