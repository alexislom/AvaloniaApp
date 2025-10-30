using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace AvaloniaApp.Converters;

public class SafeDoubleConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value?.ToString() ?? string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        // parameter передаём старое значение
        double oldValue = parameter is double d ? d : 0.0;

        if (value is string s && double.TryParse(s, out var result))
        {
            return result;
        }

        return oldValue; // если не число, возвращаем старое значение
    }
}