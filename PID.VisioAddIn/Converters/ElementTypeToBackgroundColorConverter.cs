using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using AE.PID.Models;

namespace AE.PID.Converters;

public class ElementTypeToBackgroundColorConverter : IValueConverter
{
    public object Convert(object o, Type type, object parameter, CultureInfo culture)
    {
        if (o is ElementType elementType)
            return elementType switch
            {
                ElementType.FunctionalGroup => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFDC5E")),
                ElementType.Unit => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFCAD4")),
                ElementType.Equipment => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B0D0D3")),
                ElementType.Instrument => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#C08497")),
                ElementType.FunctionalElement =>
                    new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F7AF9D")),
                _ => new SolidColorBrush(Colors.Transparent)
            };

        return new SolidColorBrush(Colors.Transparent);
    }

    public object ConvertBack(object o, Type type, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}