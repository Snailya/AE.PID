namespace AE.PID.Models.EventArgs;

public class ElementSelectedEventArgs(string name)
{
    public string Name { get; } = name;
}