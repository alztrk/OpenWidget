using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace OpenWidget.Converters;

public class IconKeyToGeometryConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string key)
        {
            var geoKey = key.StartsWith("Icon") ? "Geo" + key.Substring(4) : key;
            if (Application.Current.Resources.Contains(geoKey))
            {
                return Application.Current.Resources[geoKey];
            }
            if (Application.Current.Resources.Contains(key))
            {
                return Application.Current.Resources[key];
            }
        }
        return Application.Current.Resources["GeoCpu"];
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
