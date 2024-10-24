using System;
using System.Collections.Generic;
using System.Linq;
using AE.PID.Core.Models;
using AE.PID.Visio.UI.Avalonia.ViewModels;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Styling;

namespace AE.PID.Visio.UI.Avalonia.Converters;

public abstract class SimpleConverters
{
    /// <summary>
    ///     Check if it is a function zone or group.
    /// </summary>
    public static FuncValueConverter<FunctionLocationPropertiesViewModel?, bool> IsZoneOrGroup { get; } =
        new(vm => vm?.FunctionType is FunctionType.ProcessZone or FunctionType.FunctionGroup);

    /// <summary>
    ///     Check if it is a function zone.
    /// </summary>
    public static FuncValueConverter<FunctionLocationPropertiesViewModel?, bool> IsZone { get; } =
        new(vm => vm?.FunctionType is FunctionType.ProcessZone);

    /// <summary>
    ///     Check if it is not a equipment or instrument.
    /// </summary>
    public static FuncValueConverter<FunctionLocationPropertiesViewModel?, bool>
        IsNotEquipmentOrInstrumentOrFunctionElement { get; } =
        new(vm => vm?.FunctionType is not (FunctionType.Instrument or FunctionType.Equipment
            or FunctionType.FunctionElement));

    public static FuncValueConverter<int?, int?> CultureIndexConverter { get; } =
        new(value => value + 1);

    public static FuncValueConverter<DateTime?, string> LastSyncedText { get; } =
        new(time => time == null ? string.Empty : $"上次同步: {time:yyyy-M-d hh:mm:ss}");

    public static FuncValueConverter<int?, double> GetMinHeight { get; } =
        new(count => ((double)(count <= 10 ? count : 10) + 1) * 32);

    public static FuncValueConverter<string, string> AppendColon { get; } = new(label => $"{label}: ");

    public static FuncMultiValueConverter<string?, string> IsPropertyReadOnly { get; } =
        new(num => $"Your numbers are: '{string.Join(", ", num)}'");

    public static FuncValueConverter<DataGridRow?, int?> IndexConverter { get; } = new(row => row?.GetIndex() + 1);

    public static FuncValueConverter<ThemeVariant?, Color?> ThemeToColorConverter { get; } =
        new(theme => (string)theme?.Key == "Dark" ? Colors.Black : Colors.White);


    public static FuncValueConverter<ValueTuple<string, string>?, string> FormatPasteCommandLabel { get; } =
        new(tuple => tuple != null && !string.IsNullOrEmpty(tuple.Value.Item2) ? $"粘贴：{tuple.Value.Item2}" : "粘贴");

    public static FuncValueConverter<IEnumerable<MaterialPropertyViewModel>?, IEnumerable<MaterialPropertyViewModel>?>
        UsefulProperties { get; } =
        new(list => list?.Where(x => !string.IsNullOrEmpty(x.Value)));
}