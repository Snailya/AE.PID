using System.Text.RegularExpressions;

namespace AE.PID.Client.Core.VisioExt.Control;

[AttributeUsage(AttributeTargets.Property)]
public class ShapeSheetCell(string cell, bool useFormatValue = false) : Attribute
{
    public ShapeSheetCell(string cell, string pattern) : this(cell)
    {
        Regex = new Regex(pattern);
    }

    public string CellName { get; } = cell; // 必填参数（通过构造函数设置）
    public bool UseFormatValue { get; } = useFormatValue;
    public Regex? Regex { get; set; }
}