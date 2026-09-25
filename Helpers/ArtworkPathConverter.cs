using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;

namespace NewWinampClassic.Helpers;

public class ArtworkPathConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is string path && !string.IsNullOrEmpty(path) && System.IO.File.Exists(path))
            return new BitmapImage(new Uri(path));
        return null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}
