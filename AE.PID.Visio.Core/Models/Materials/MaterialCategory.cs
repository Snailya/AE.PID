namespace AE.PID.Visio.Core.Models;

public class MaterialCategory
{
    public int Id { get; set; }
    public int ParentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
}