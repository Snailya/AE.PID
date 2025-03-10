using System;
using System.Collections.Generic;
using System.Linq;
using AE.PID.Client.UI.Avalonia.Shared;
using AE.PID.Core;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;

namespace AE.PID.Client.UI.Avalonia;

public class SimpleConverters
{
    public static FuncValueConverter<DateTime?, string> LastSyncedText { get; } =
        new(time => time == null ? string.Empty : $"上次同步: {time:yyyy-M-d hh:mm:ss}");

    /// <summary>
    ///     Compute the minimum height used for better displaying of data grid.
    ///     If the row count is smaller than 10, use the actual height needed to display the row, otherwise only display 10
    ///     rows.
    /// </summary>
    public static FuncValueConverter<int?, double> GetMinHeightForDataGrid { get; } =
        new(count => ((double)(count <= 10 ? count : 10) + 1) * 32 + 8);

    public static FuncValueConverter<ValueTuple<string, string>?, string> FormatPasteCommandLabel { get; } =
        new(tuple => tuple != null && !string.IsNullOrEmpty(tuple.Value.Item2) ? $"粘贴：{tuple.Value.Item2}" : "粘贴");

    public static FuncValueConverter<IEnumerable<MaterialPropertyViewModel>?, IEnumerable<MaterialPropertyViewModel>?>
        UsefulProperties { get; } =
        new(list => list?.Where(x => !string.IsNullOrEmpty(x.Value)));

    /// <summary>
    ///     Highlight the text color based on synchronized status
    /// </summary>
    public static FuncValueConverter<SyncStatus, IBrush> SyncStatusToTextColorConverter { get; } =
        new(status => status switch
        {
            SyncStatus.Added => new SolidColorBrush(Color.Parse("#0A7700")),
            SyncStatus.Modified => new SolidColorBrush(Color.Parse("#0032A0")),
            SyncStatus.Deleted => new SolidColorBrush(Color.Parse("#616161")),
            // SyncStatus.Unchanged => SystemColors.WindowText,
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
        });


    public static FuncValueConverter<DataGridColumnHeader?, ByGroupViewModel?>
        DataGridColumnHeaderToBindingPath { get; } =
        new(header =>
        {
            if (header == null) return null;

            var dataGrid = header.FindAncestorOfType<DataGrid>()!;
            var headersPresenter =
                header.GetVisualParent<DataGridColumnHeadersPresenter>()!;
            var index = headersPresenter.Children.Count == dataGrid.Columns.Count + 1
                ? headersPresenter.Children.IndexOf(header)
                : headersPresenter.Children.IndexOf(header) - 1;

            if (dataGrid.Columns[index] is not DataGridBoundColumn dataGridColumn) return null;

            return dataGridColumn.Binding switch
            {
                Binding binding => new ByGroupViewModel
                {
                    Name = header.Content?.ToString() ?? string.Empty,
                    PropertyName = binding.Path
                },
                CompiledBindingExtension compiledBindingExtension => new ByGroupViewModel
                {
                    Name = header.Content?.ToString() ?? string.Empty,
                    PropertyName = compiledBindingExtension.Path.ToString()
                },
                _ => null
            };
        });

    public static FuncValueConverter<int, bool> NotZero { get; } = new(value => value != 0);


    public static FuncValueConverter<ThemeVariant?, IBrush?> ThemeToSplitterColorBrushConverter { get; } =
        new(theme =>
            new SolidColorBrush(theme == null || (string)theme.Key == "Light" ? Colors.LightGray : Colors.DarkGray));

    /// <summary>
    ///     Highlight the optional function group with color #800000 (red)
    /// </summary>
    public static FuncValueConverter<bool, object?> FunctionGroupOptionalToTextColor { get; } =
        new(isOptional => isOptional
            ? new SolidColorBrush(Color.Parse("#800000"))
            : AvaloniaProperty.UnsetValue);


    #region -- Function Types --

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

    #endregion
}