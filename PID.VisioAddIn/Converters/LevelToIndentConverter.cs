﻿using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace AE.PID.Converters;

public class LevelToIndentConverter : IValueConverter
{
    private const double c_IndentSize = 25.0;

    public object Convert(object o, Type type, object parameter, CultureInfo culture)
    {
        return new Thickness((int)o * c_IndentSize, 0, 0, 0);
    }

    public object ConvertBack(object o, Type type, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}