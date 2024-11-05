namespace AE.PID.Visio.Core.Models;

public class VisioMaster(string baseId, string name)
{
    public string BaseId { get; private set; } = baseId;

    public string Name { get; set; } = name;
}