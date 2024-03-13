namespace AE.PID.Core.DTOs;

public class MaterialCategoryDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Code { get; set; }
    public int ParentId { get; set; }
}