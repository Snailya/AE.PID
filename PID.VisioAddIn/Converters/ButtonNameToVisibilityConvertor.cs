using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using AE.PID.Views.Windows;

namespace AE.PID.Converters;

public class ButtonNameToVisibilityConvertor : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length > 2)
            throw new ArgumentOutOfRangeException();

        if (values[0] is string name && values[1] is WindowBase.WindowButton windowButton)
            return windowButton switch
            {
                WindowBase.WindowButton.CloseOnly => name == "PART_Close" ? Visibility.Visible : Visibility.Collapsed,
                WindowBase.WindowButton.Normal => Visibility.Visible,
                WindowBase.WindowButton.None => Visibility.Collapsed,
                _ => throw new ArgumentOutOfRangeException()
            };

        throw new ArgumentException();
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}