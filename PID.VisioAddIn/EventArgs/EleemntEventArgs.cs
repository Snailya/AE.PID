using AE.PID.Models;

namespace AE.PID.EventArgs;

public class ElementSelectedEventArgs(ElementBase elementBase)
{
    public ElementBase ElementBase { get; } = elementBase;
}