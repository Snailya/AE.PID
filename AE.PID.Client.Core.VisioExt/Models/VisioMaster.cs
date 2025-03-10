namespace AE.PID.Client.Core.VisioExt;

public class VisioMaster(string baseId, string name, string uniqueId)
{
    public VisioMasterId Id { get; set; } = new(baseId, uniqueId);

    /// <summary>
    ///     The name attribute of the visio master
    /// </summary>
    public string Name { get; set; } = name;
}