using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace AE.PID.Converters;

public class SecondaryWindowLeftConvertor : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double left && parameter is Window owner) return left + owner.ActualWidth;

        throw new InvalidOperationException();
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double left && parameter is Window owner)
            return left - owner.ActualWidth;

        throw new InvalidOperationException();
    }
}