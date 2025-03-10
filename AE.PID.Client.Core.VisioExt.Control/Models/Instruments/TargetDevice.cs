namespace AE.PID.Client.Core.VisioExt.Control;

[ElectricalControlSpecificationItem]
public class TargetDevice
{
    /// <summary>
    ///     受控设备
    /// </summary>
    [ShapeSheetCell(CellDict.Description)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    ///     受控设备编号
    /// </summary>
    [ShapeSheetCell(CellDict.Tag)]
    public string ControlTag { get; set; } = string.Empty;

    /// <summary>
    ///     控制功能
    /// </summary>
    public string AggregationFunction { get; set; } = string.Empty;
}