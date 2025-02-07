namespace AE.PID.Server.Models;

public class VisioMaster
{
    public string UniqueId { get; set; }
    public string BaseId { get; set; }
    public string Name { get; set; }

    public override string ToString()
    {
        return $"{Name} ({BaseId}, {UniqueId})";
    }
}