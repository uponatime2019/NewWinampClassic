using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;

namespace NewWinampClassic.Helpers;

public class FavoriteColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is true)
        {
            // Premium golden amber for stars
            return new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 193, 7));
        }

        // WinUI 3 secondary text color (subtle/harmonious)
        if (Application.Current.Resources.TryGetValue("TextFillColorSecondaryBrush", out var brushObj) && brushObj is Brush brush)
        {
            return brush;
        }

        return new SolidColorBrush(Windows.UI.Color.FromArgb(255, 128, 128, 128));
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}
