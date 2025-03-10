using ClosedXML.Attributes;

namespace AE.PID.Client.Core.VisioExt.Control.Valves;

[ElectricalControlSpecificationItem("5.阀类设备", "4.1 水阀",
[
    BaseIdDict.Valve, BaseIdDict.CheckValve, BaseIdDict.SafetyValve, BaseIdDict.RegulatingValve, BaseIdDict.OtherValve
])]
public class Valve : ElectricalControlSpecificationItemBase
{
    public override Type Type { get; } = typeof(Valve);

    /// <summary>
    ///     电压类型
    /// </summary>
    [XLColumn(Order = 10)]
    [ShapeSheetCell("VoltageType")]
    public string VoltageType { get; set; } = string.Empty;

    /// <summary>
    ///     额定电压[V]
    /// </summary>
    [ShapeSheetCell("Voltage")]
    [XLColumn(Order = 11)]
    public double RatedVoltage { get; set; }

    /// <summary>
    ///     频率[Hz]
    /// </summary>
    [XLColumn(Order = 12)]
    public double Frequency { get; set; }

    /// <summary>
    ///     阀门类型
    /// </summary>
    [XLColumn(Order = 13)]
    public string Category { get; set; } = string.Empty;

    /// <summary>
    ///     控制输入
    /// </summary>
    [XLColumn(Order = 14)]
    public string Input { get; set; } = string.Empty;

    /// <summary>
    ///     反馈输出
    /// </summary>
    [XLColumn(Order = 15)]
    public string Output { get; set; } = string.Empty;

    /// <summary>
    ///     到位反馈
    /// </summary>
    [XLColumn(Order = 16)]
    public bool Feedback { get; set; }

    /// <summary>
    ///     阀门常态
    /// </summary>
    [XLColumn(Order = 17)]
    public string NormalStatus { get; set; } = string.Empty;

    /// <summary>
    ///     执行器数量
    /// </summary>
    [XLColumn(Order = 18)]
    public double NumberOfActuators { get; set; }

    /// <summary>
    ///     单控/双控
    /// </summary>
    [XLColumn(Order = 19)]
    public string NumberOfControls { get; set; } = string.Empty;

    /// <summary>
    ///     是否防爆
    /// </summary>
    [XLColumn(Order = 20)]
    public bool IsExplosionProof { get; set; }

    /// <summary>
    ///     阀门控制信号来源
    /// </summary>
    [XLColumn(Order = 21)]
    public bool SourceSignal { get; set; }

    /// <summary>
    ///     控制要求
    /// </summary>
    [XLColumn(Order = 22)]
    public bool ControlRequirement { get; set; }

    /// <summary>
    ///     控制前提条件
    /// </summary>
    [XLColumn(Order = 23)]
    public bool ControlPrerequisite { get; set; }

    /// <summary>
    ///     安装位置
    /// </summary>
    [XLColumn(Order = 24)]
    public string? InstallationPosition { get; set; }

    /// <summary>
    ///     采购
    /// </summary>
    [XLColumn(Order = 25)]
    public string? PurchasedBy { get; set; }

    /// <summary>
    ///     控制
    /// </summary>
    [XLColumn(Order = 26)]
    public string? Control { get; set; }
}