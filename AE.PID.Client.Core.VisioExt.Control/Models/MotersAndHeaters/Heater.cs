using AE.PID.Core;
using ClosedXML.Attributes;

namespace AE.PID.Client.Core.VisioExt.Control;

[ElectricalControlSpecificationItem("3.电机和加热器类", "2.4 电加热器", [BaseIdDict.Heater])]
public class Heater : ElectricalControlSpecificationItemBase
{
    public override Type Type { get; } = typeof(Heater);

    /// <summary>
    ///     单根电阻丝功率[kW]
    /// </summary>
    [XLColumn(Order = 10)]
    public double? PowerOfSingleResistanceWire { get; set; }

    /// <summary>
    ///     单组电阻丝根数
    /// </summary>
    [XLColumn(Order = 11)]
    public double? NumberOfResistanceWiresPerGroup { get; set; }

    /// <summary>
    ///     电加热组数
    /// </summary>
    [XLColumn(Order = 13)]
    public double? NumberOfHeatingGroups { get; set; }

    /// <summary>
    ///     额定总功率[kW]
    /// </summary>
    [XLColumn(Order = 14)]
    public double? RatedTotalPower { get; set; }

    /// <summary>
    ///     运行功率[kW]
    /// </summary>
    [ShapeSheetCell("Prop.Power")]
    [XLColumn(Order = 15)]
    public double? OperatingPower { get; set; }

    /// <summary>
    ///     电压类型
    /// </summary>
    [ShapeSheetCell("Prop.VoltageType")]
    [XLColumn(Order = 17)]
    public string? VoltageType { get; set; }

    /// <summary>
    ///     额定电压[V]
    /// </summary>
    [ShapeSheetCell("Prop.Voltage")]
    [XLColumn(Order = 18)]
    public double? RatedVoltage { get; set; }

    /// <summary>
    ///     额定电流[A]
    /// </summary>
    [XLColumn(Order = 19)]
    public double? RatedCurrent { get; set; }

    /// <summary>
    ///     频率[Hz]
    /// </summary>
    [XLColumn(Order = 20)]
    public double? Frequency { get; set; }

    /// <summary>
    ///     变频控制
    /// </summary>
    [XLColumn(Order = 24)]
    public bool? VariableFrequencyControl { get; set; }

    /// <summary>
    ///     备用电源
    /// </summary>
    [XLColumn(Order = 25)]
    public bool? BackupPowerSupply { get; set; }

    /// <summary>
    ///     配维修开关
    /// </summary>
    [XLColumn(Order = 26)]
    public bool? EquippedWithMaintenanceSwitch { get; set; }

    /// <summary>
    ///     热敏保护
    /// </summary>
    [XLColumn(Order = 27)]
    public bool? ThermalProtection { get; set; }

    /// <summary>
    ///     标高
    /// </summary>
    [XLColumn(Order = 29)]
    [ShapeSheetCell("Prop.Level")]
    public double? Level { get; set; }
}