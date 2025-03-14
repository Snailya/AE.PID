using System.Text.Json.Serialization;

namespace AE.PID.Core;

public class MaterialCategoryDto : ITreeNode
{
    public string Code { get; set; } = string.Empty;
    public int Id { get; set; }
    public int ParentId { get; set; }
    [JsonPropertyName("name")] public string NodeName { get; set; } = string.Empty;
}