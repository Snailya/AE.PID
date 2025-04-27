using System.ComponentModel.DataAnnotations;
using AE.PID.Core;
using ClosedXML.Attributes;

namespace AE.PID.Client.Core.VisioExt.Control;

[ElectricalControlSpecificationItem([BaseIdDict.FunctionElement])]
public class Motor : ElectricalControlSpecificationItemBase
{
    public override Type Type { get; } = typeof(Motor);

    /// <summary>
    ///     关联设备号
    /// </summary>
    [XLColumn(Ignore = true)]
    [ShapeSheetCell(CellDict.RefEquipment)]
    [Required]
    public string RefEquipment { get; set; }

    public override string FullDesignation => $"={Zone}++{Group}+{Group}-{RefEquipment}-{Designation}";
}