using System;
using System.Collections;
using System.Globalization;
using System.Linq;
using Avalonia.Data.Converters;

namespace AE.PID.Client.UI.Avalonia;

public class IsNotEqualConverter : IValueConverter
{
    public static readonly IsNotEqualConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null && parameter == null)
            return false;
        if (value == null || parameter == null)
            return true;
        return !(value.Equals(parameter) || value.ToString() == parameter.ToString());
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

public class IsInConverter : IValueConverter
{
    public static readonly IsNotEqualConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null) return false;
        if (parameter is not IEnumerable customArray) return value;
        return customArray.Cast<object?>().Contains(value) ? true : value;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}