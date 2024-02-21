using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using AE.PID.Models.BOM;

namespace AE.PID.Converters;

public class LineItemTypeToSymbolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is LineItemType type)
            return type switch
            {
                LineItemType.UnitEquipment => "U",
                LineItemType.SingleEquipment => "E",
                LineItemType.AttachedEquipment => "A",
                _ => throw new ArgumentOutOfRangeException()
            };

        throw new ArgumentOutOfRangeException();
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}