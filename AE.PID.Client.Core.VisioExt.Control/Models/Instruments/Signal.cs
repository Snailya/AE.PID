using AE.PID.Core;
using ClosedXML.Attributes;

namespace AE.PID.Client.Core.VisioExt.Control;

[ElectricalControlSpecificationItem([BaseIdDict.Signal])]
public class Signal : ElectricalControlSpecificationItemBase
{
    public override Type Type { get; } = typeof(Signal);

    /// <summary>
    ///     控制功能1
    /// </summary>
    [ShapeSheetCell(CellDict.Function1)]
    [XLColumn(Ignore = true)]

    public string? Function1 { get; set; }

    /// <summary>
    ///     控制功能2
    /// </summary>
    [ShapeSheetCell(CellDict.Function2)]
    [XLColumn(Ignore = true)]

    public string? Function2 { get; set; }

    /// <summary>
    ///     控制功能3
    /// </summary>
    [ShapeSheetCell(CellDict.Function3)]
    [XLColumn(Ignore = true)]

    public string? Function3 { get; set; }

    /// <summary>
    ///     控制功能
    /// </summary>
    public string AggregationFunction =>
        string.Join("，", new[] { Function1, Function2, Function3 }.Where(x => !string.IsNullOrEmpty(x)));

    /// <summary>
    ///     连接的设备
    /// </summary>
    [Connected(null, [BaseIdDict.Instrument, BaseIdDict.Switch])]
    [XLColumn(Ignore = true)]
    public ElectricalControlSpecificationItemBase? TargetDevice { get; set; }
}