using System;
using System.Globalization;
using System.Windows.Data;
using AE.PID.Models.BOM;

namespace AE.PID.Converters;

public class ElementTypeToStringConverter : IValueConverter
{
    public object Convert(object o, Type type, object parameter, CultureInfo culture)
    {
        if (o is ElementType elementType)
            return elementType switch
            {
                ElementType.FunctionalGroup => "FG",
                ElementType.Unit => "U",
                ElementType.Equipment => "E",
                ElementType.Instrument => "I",
                ElementType.FunctionalElement =>
                    "FE",
                _ => string.Empty
            };

        return string.Empty;
    }

    public object ConvertBack(object o, Type type, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}