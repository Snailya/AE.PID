using AE.PID.Core;
using ClosedXML.Attributes;

namespace AE.PID.Client.Core.VisioExt.Control;

[ElectricalControlSpecificationItem("3.电机和加热器类", "2.2 风机", [BaseIdDict.Fan])]
public class Fan : ElectricalControlSpecificationItemBase
{
    public override Type Type { get; } = typeof(Fan);

    /// <summary>
    ///     流量[m3/h]
    /// </summary>
    [ShapeSheetCell("Prop.AirVolume")]
    [XLColumn(Order = 10)]
    public double? FlowRate { get; set; }

    /// <summary>
    ///     静压[Pa]
    /// </summary>
    [ShapeSheetCell("Prop.StaticPressure")]
    [XLColumn(Order = 11)]
    public double? Pressure { get; set; }

    /// <summary>
    ///     压力环[Pa]
    /// </summary>
    [XLColumn(Order = 12)]
    public double? FanPressureRing { get; set; }

    /// <summary>
    ///     转速[r/min]
    /// </summary>
    [ShapeSheetCell("Speed")]
    [XLColumn(Order = 13)]
    public double? Speed { get; set; }

    /// <summary>
    ///     额定功率[kW]
    /// </summary>
    [ShapeSheetCell("Prop.Power")]
    [XLColumn(Order = 14)]
    public double? RatedPower { get; set; }

    /// <summary>
    ///     轴功率[kW]
    /// </summary>
    [XLColumn(Order = 15)]
    public double? ShaftPower { get; set; }

    // /// <summary>
    // ///     电机级数
    // /// </summary>
    // [XLColumn(Order = 16)]
    // public int? MotorPoleNumber { get; set; }

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
    [ShapeSheetCell("Prop.Current")]
    [XLColumn(Order = 19)]
    public double? RatedCurrent { get; set; }

    /// <summary>
    ///     频率[Hz]
    /// </summary>
    [XLColumn(Order = 20)]
    public double? Frequency { get; set; }

    /// <summary>
    ///     电机防爆
    /// </summary>
    [ShapeSheetCell("Prop.IsExplosionProof")]
    [XLColumn(Order = 21)]
    public bool? ExplosionProofMotor { get; set; }

    /// <summary>
    ///     强冷风扇
    /// </summary>
    [XLColumn(Order = 22)]
    public bool? ForcedCoolingFan { get; set; }

    /// <summary>
    ///     变频电机
    /// </summary>
    [XLColumn(Order = 23)]
    public bool? VariableFrequencyMotor { get; set; }

    /// <summary>
    ///     变频控制
    /// </summary>
    [ShapeSheetCell("Prop.IsVariableSpeedDrive")]
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

    [XLColumn(Ignore = true)]
    [Callout(BaseIdDict.FunctionElement)]
    public Motor? Motor { get; set; }

    /// <summary>
    ///     设备代号
    /// </summary>
    [XLColumn(Order = 32)]
    public string? AttachedDesignation => Motor?.Designation;

    /// <summary>
    ///     设备功能描述
    /// </summary>
    [XLColumn(Order = 33)]
    public string? AttachedDescription => Motor?.Description;

    /// <summary>
    ///     电控编号
    /// </summary>
    [XLColumn(Order = 34)]
    public string? AttachedFullDesignation => Motor?.FullDesignation;
}