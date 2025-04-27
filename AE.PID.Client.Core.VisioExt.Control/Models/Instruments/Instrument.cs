using AE.PID.Core;
using ClosedXML.Attributes;

namespace AE.PID.Client.Core.VisioExt.Control;

[ElectricalControlSpecificationItem("4.仪表类设备", [BaseIdDict.Instrument, BaseIdDict.Switch])]
public class Instrument : ElectricalControlSpecificationItemBase
{
    private const string CustomOrder = "IRCSZAV";
    public override Type Type { get; } = typeof(Instrument);

    /// <summary>
    ///     仪表类型
    /// </summary>
    [XLColumn(Order = 10)]
    public string? Category { get; set; }

    /// <summary>
    ///     量程范围
    /// </summary>
    [XLColumn(Order = 11)]
    public string? Range { get; set; }

    /// <summary>
    ///     反馈输出
    /// </summary>
    [XLColumn(Order = 12)]
    public string? Output { get; set; }

    /// <summary>
    ///     是否防爆
    /// </summary>
    [XLColumn(Order = 13)]
    public bool? IsExplosionProof { get; set; }

    /// <summary>
    ///     是否本地显示
    /// </summary>
    [XLColumn(Order = 14)]
    public bool? IsLocalDisplay { get; set; }

    /// <summary>
    ///     信号数量
    /// </summary>
    [XLColumn(Order = 15)]
    public int? NumberOfSignals { get; set; }

    /// <summary>
    ///     延长杆[mm]
    /// </summary>
    [XLColumn(Order = 16)]
    public double? ExtensionRodLength { get; set; }

    /// <summary>
    ///     插深[mm]
    /// </summary>
    [XLColumn(Order = 17)]
    public double? InsertionDepth { get; set; }

    /// <summary>
    ///     接触介质
    /// </summary>
    [XLColumn(Order = 18)]
    public string? Meida { get; set; }

    /// <summary>
    ///     介质温度[℃]
    /// </summary>
    [XLColumn(Order = 19)]
    public string? MediaTemperature { get; set; }

    /// <summary>
    ///     介质酸碱性
    /// </summary>
    [XLColumn(Order = 20)]
    public double? MediaPH { get; set; }

    /// <summary>
    ///     受控设备
    /// </summary>
    [XLColumn(Order = 21)]
    public string TargetDeviceTypes =>
        string.Join("\n", Signals?.Select(x => (x as Signal)?.TargetDevice?.Description) ?? []);

    /// <summary>
    ///     受控设备编号
    /// </summary>
    [XLColumn(Order = 22)]
    public string TargetDeviceTags =>
        string.Join("\n", Signals?.Select(x => (x as Signal)?.TargetDevice?.FullDesignation) ?? []);

    /// <summary>
    /// </summary>
    [ShapeSheetCell(CellDict.ProcessVariableAndControlFunctions, "[IRCSZAV]+$")]
    [XLColumn(Ignore = true)]
    public string? ControlFunctions { get; set; }

    /// <summary>
    ///     高位输出/输入功能指示1
    /// </summary>
    [ShapeSheetCell(CellDict.High1)]
    [XLColumn(Ignore = true)]
    public string? High1 { get; set; }

    /// <summary>
    ///     高位输出/输入功能指示2
    /// </summary>
    [ShapeSheetCell(CellDict.High2)]
    [XLColumn(Ignore = true)]
    public string? High2 { get; set; }

    /// <summary>
    ///     高位输出/输入功能指示3
    /// </summary>
    [ShapeSheetCell(CellDict.High3)]
    [XLColumn(Ignore = true)]
    public string? High3 { get; set; }

    /// <summary>
    ///     低位输出/输入功能指示1
    /// </summary>
    [ShapeSheetCell(CellDict.Low1)]
    [XLColumn(Ignore = true)]
    public string? Low1 { get; set; }

    /// <summary>
    ///     低位输出/输入功能指示2
    /// </summary>
    [ShapeSheetCell(CellDict.Low2)]
    [XLColumn(Ignore = true)]
    public string? Low2 { get; set; }

    /// <summary>
    ///     低位输出/输入功能指示3
    /// </summary>
    [ShapeSheetCell(CellDict.Low3)]
    [XLColumn(Ignore = true)]
    public string? Low3 { get; set; }

    /// <summary>
    ///     控制功能1
    /// </summary>
    [XLColumn(Order = 23)]
    public string Function1 => string.Join("、",
        (ControlFunctions?.Select(x => x.ToString()) ?? [])
        .OrderBy(x => CustomOrder.IndexOf(x, StringComparison.Ordinal))
        .Concat(new[] { High1, High2, High3, Low1, Low2, Low3 }.Where(x => !string.IsNullOrEmpty(x))
            .Select(x => x!.Substring(0, 1)))
        .Select(ControlFunctionsToText)); // 2025.3.14： 高低位只允许写A、S、Z，此处不做校验，如果后面有需求再在attribute中用正则表示。

    /// <summary>
    ///     控制功能2
    /// </summary>
    [XLColumn(Order = 24)]
    public string Function2 => string.Join("\n", Signals?.Cast<Signal>().Select(x => x.AggregationFunction) ?? []);

    /// <summary>
    ///     运行模式
    /// </summary>
    [XLColumn(Order = 25)]
    public string? OperationMode { get; set; }

    /// <summary>
    ///     安装位置
    /// </summary>
    [XLColumn(Order = 26)]
    public string? InstallationPosition { get; set; }

    /// <summary>
    ///     采购
    /// </summary>
    [XLColumn(Order = 27)]
    public string? PurchasedBy { get; set; }

    [XLColumn(Ignore = true)]
    [Connected([BaseIdDict.Signal])]
    public IEnumerable<ElectricalControlSpecificationItemBase>? Signals { get; set; }

    private static string ControlFunctionsToText(string letter)
    {
        return letter switch
        {
            "I" => "指示",
            "R" => "记录",
            "C" => "控制",
            "S" => "开关",
            "Z" => "开关（安全）",
            "A" => "报警",
            "V" => "可视化",
            _ => letter
        };
    }
}