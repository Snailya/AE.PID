using AE.PID.Interfaces;

namespace AE.PID.Dtos;

public class MaterialCategoryDto : ITreeNode
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public int Id { get; set; }
    public int ParentId { get; set; }
}