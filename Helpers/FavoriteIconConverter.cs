using Microsoft.UI.Xaml.Data;

namespace NewWinampClassic.Helpers;

public class FavoriteIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is true)
            return "\uE735"; // Filled star (FavoriteStarFill)
        return "\uE734"; // Outline star (FavoriteStar)
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}
