using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace AE.PID.Converters;

public class SideWindowMaxWidthConvertor : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length != 2 || values[0] is not double v1 || values[1] is not double v2)
            return SystemParameters.WorkArea.Width;
        
        var maxWidth = SystemParameters.WorkArea.Width - v1 - v2;
        return maxWidth;

    }


    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}