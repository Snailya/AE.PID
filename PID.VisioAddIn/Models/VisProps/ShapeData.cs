using AE.PID.Interfaces;

namespace PID.VisioAddIn.Props;

public class ShapeData(
    string name,
    string label,
    string format,
    string value,
    int type = 0,
    int sortKey = 0,
    bool invisible = false)
    : ValueProp(name, "Prop", value), IShapeData
{
    public string Label { get; } = label;
    public string Format { get; } = format;
    public string Type { get; set; } = type.ToString();
    public string SortKey { get; set; } = $"\"{sortKey}\"";
    public string Invisible { get; set; } = invisible.ToString().ToUpper();
}