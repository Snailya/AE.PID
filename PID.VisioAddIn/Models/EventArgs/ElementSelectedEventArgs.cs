using AE.PID.Models.BOM;

namespace AE.PID.Models.EventArgs;

public class ElementSelectedEventArgs(Element element)
{
    public Element Element { get; } = element;
}