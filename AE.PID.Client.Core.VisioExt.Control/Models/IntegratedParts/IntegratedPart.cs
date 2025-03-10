using ClosedXML.Attributes;

namespace AE.PID.Client.Core.VisioExt.Control;

[ElectricalControlSpecificationItem("7.成套设备", "7. 成套设备",
    [])]
public class IntegratedPart : ElectricalControlSpecificationItemBase
{
    public override Type Type { get; } = typeof(IntegratedPart);

    /// <summary>
    ///     额定功率[kW]
    /// </summary>
    [ShapeSheetCell("Prop.Power")]
    [XLColumn(Order = 10)]
    public double? RatedPower { get; set; }

    /// <summary>
    ///     运行功率[kW]
    /// </summary>
    [XLColumn(Order = 11)]
    public double? OperatingPower { get; set; }

    /// <summary>
    ///     电压类型
    /// </summary>
    [ShapeSheetCell("Prop.VoltageType")]
    [XLColumn(Order = 12)]
    public string? VoltageType { get; set; }

    /// <summary>
    ///     额定电压[V]
    /// </summary>
    [XLColumn(Order = 13)]
    [ShapeSheetCell("Prop.Voltage")]
    public double? RatedVoltage { get; set; }

    /// <summary>
    ///     额定电流[A]
    /// </summary>
    [ShapeSheetCell("Prop.Current")]
    [XLColumn(Order = 14)]
    public double? RatedCurrent { get; set; }

    /// <summary>
    ///     频率[Hz]
    /// </summary>
    [XLColumn(Order = 15)]
    public double? Frequency { get; set; }

    /// <summary>
    ///     是否防爆
    /// </summary>
    [XLColumn(Order = 16)]
    public bool? IsExplosionProof { get; set; }

    /// <summary>
    ///     备用电源
    /// </summary>
    [XLColumn(Order = 17)]
    public bool? BackupPowerSupply { get; set; }

    /// <summary>
    ///     标高
    /// </summary>
    [XLColumn(Order = 19)]
    [ShapeSheetCell("Prop.Level")]
    public double? Level { get; set; }

    /// <summary>
    ///     采购
    /// </summary>
    [XLColumn(Order = 20)]
    public string? PurchasedBy { get; set; }

    /// <summary>
    ///     控制
    /// </summary>
    [XLColumn(Order = 21)]
    public string? Control { get; set; }
}