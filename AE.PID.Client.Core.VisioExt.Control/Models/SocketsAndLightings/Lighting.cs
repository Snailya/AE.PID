using ClosedXML.Attributes;

namespace AE.PID.Client.Core.VisioExt.Control.SocketsAndLightings;

[ElectricalControlSpecificationItem("6.插座和照明", "5.2 照明", [BaseIdDict.Lighting])]
public class Lighting : ElectricalControlSpecificationItemBase
{
    public override Type Type { get; } = typeof(Lighting);

    /// <summary>
    ///     额定功率[kW]
    /// </summary>
    [ShapeSheetCell("Prop.Power")]
    [XLColumn(Order = 10)]
    public double? RatedPower { get; set; }

    /// <summary>
    ///     电压类型
    /// </summary>
    [ShapeSheetCell("Prop.VoltageType")]
    [XLColumn(Order = 11)]
    public string? VoltageType { get; set; }

    /// <summary>
    ///     额定电压[V]
    /// </summary>
    [ShapeSheetCell("Prop.Voltage")]
    [XLColumn(Order = 12)]
    public double? RatedVoltage { get; set; }

    /// <summary>
    ///     额定电流[A]
    /// </summary>
    [ShapeSheetCell("Prop.Current")]
    [XLColumn(Order = 13)]
    public double? RatedCurrent { get; set; }

    /// <summary>
    ///     频率[Hz]
    /// </summary>
    [XLColumn(Order = 14)]
    public double? Frequency { get; set; }

    /// <summary>
    ///     灯管类型
    /// </summary>
    [XLColumn(Order = 15)]
    [ShapeSheetCell("Prop.TypeOfLamp")]
    public string? TubeType { get; set; }

    /// <summary>
    ///     单个灯箱灯管数量
    /// </summary>
    [XLColumn(Order = 16)]
    [ShapeSheetCell("Prop.NumOfTubesPerLamp")]
    public int? NumberOfTubes { get; set; }

    /// <summary>
    ///     单支灯管功率[W]
    /// </summary>
    [XLColumn(Order = 17)]
    [ShapeSheetCell("Prop.UnitPowerOfLamp")]
    public double? PowerOfTube { get; set; }

    /// <summary>
    ///     照明分组
    /// </summary>
    [XLColumn(Order = 18)]
    public string? LightingGrouping { get; set; }

    /// <summary>
    ///     接线方式
    /// </summary>
    [XLColumn(Order = 19)]
    public string? WiringMethod { get; set; }

    /// <summary>
    ///     控制模式
    /// </summary>
    [XLColumn(Order = 20)]
    public string? ControlMode { get; set; }

    /// <summary>
    ///     是否防爆
    /// </summary>
    [XLColumn(Order = 21)]
    public bool IsExplosionProof { get; set; }

    /// <summary>
    ///     是否调光
    /// </summary>
    [XLColumn(Order = 22)]
    public bool? IsAdjustable { get; set; }
}