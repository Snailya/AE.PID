using ClosedXML.Attributes;

namespace AE.PID.Client.Core.VisioExt.Control;

public class UPS : ElectricalControlSpecificationItemBase
{
    public override Type Type { get; } = typeof(UPS);

    /// <summary>
    ///     额定容量[kVA]
    /// </summary>
    [XLColumn(Order = 14)]
    public double? RateCapacity { get; set; }

    /// <summary>
    ///     电压类型
    /// </summary>
    [ShapeSheetCell("Prop.VoltageType")]
    [XLColumn(Order = 17)]
    public string? VoltageType { get; set; } = string.Empty;

    /// <summary>
    ///     额定电压[V]
    /// </summary>
    [ShapeSheetCell("Prop.Voltage")]
    [XLColumn(Order = 18)]
    public double? RatedVoltage { get; set; }

    /// <summary>
    ///     额定电流[A]
    /// </summary>
    [ShapeSheetCell("Prop.Current")]
    [XLColumn(Order = 19)]
    public double? RatedCurrent { get; set; }

    /// <summary>
    ///     频率[Hz]
    /// </summary>
    [XLColumn(Order = 20)]
    public double? Frequency { get; set; }

    /// <summary>
    ///     标高
    /// </summary>
    [XLColumn(Order = 29)]
    [ShapeSheetCell("Prop.Level")]
    public double? Level { get; set; }
}