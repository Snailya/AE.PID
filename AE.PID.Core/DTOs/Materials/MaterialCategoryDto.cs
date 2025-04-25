using System.ComponentModel;
using System.Text.Json.Serialization;

namespace AE.PID.Core;

public class MaterialCategoryDto : ITreeNode
{
    [Description("编码")] public string Code { get; set; } = string.Empty;
    [Description("ID")] public int Id { get; set; }
    [Description("上级ID")] public int ParentId { get; set; }

    [JsonPropertyName("name")]
    [Description("名称")]
    public string NodeName { get; set; } = string.Empty;
}