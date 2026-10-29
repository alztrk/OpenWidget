using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace RigBadge.Converters;

public class KeyToBrushConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string key && !string.IsNullOrEmpty(key))
        {
            if (Application.Current.Resources.Contains(key))
            {
                return Application.Current.Resources[key];
            }
        }
        return Application.Current.Resources["AccentBrush"] as Brush ?? Brushes.DodgerBlue;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
