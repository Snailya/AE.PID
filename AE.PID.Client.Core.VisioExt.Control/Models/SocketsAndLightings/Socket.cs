using AE.PID.Core;
using ClosedXML.Attributes;

namespace AE.PID.Client.Core.VisioExt.Control.SocketsAndLightings;

[ElectricalControlSpecificationItem("6.插座和照明", "5.1 插座", [BaseIdDict.Socket])]
public class Socket : ElectricalControlSpecificationItemBase
{
    public override Type Type { get; } = typeof(Socket);

    /// <summary>
    ///     额定功率[kW]
    /// </summary>
    [ShapeSheetCell("Pro.Power")]
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
    ///     孔数
    /// </summary>
    [XLColumn(Order = 15)]
    [ShapeSheetCell("Prop.NumOfHoles")]
    public int? NumberOfHoles { get; set; }

    /// <summary>
    ///     是否防爆
    /// </summary>
    [XLColumn(Order = 21)]
    public bool IsExplosionProof { get; set; }

    /// <summary>
    ///     安装位置
    /// </summary>
    [XLColumn(Order = 22)]
    public string? InstallationPosition { get; set; }
}