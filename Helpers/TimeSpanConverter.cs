using Microsoft.UI.Xaml.Data;
using System;

namespace NewWinampClassic.Helpers;

public partial class TimeSpanConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is TimeSpan ts)
            return $"{(int)ts.TotalMinutes}:{ts.Seconds:D2}";
        if (value is double totalSeconds)
        {
            var span = TimeSpan.FromSeconds(totalSeconds);
            return $"{(int)span.TotalMinutes}:{span.Seconds:D2}";
        }
        return "0:00";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
}
