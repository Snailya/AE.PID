using System.Text.Json.Serialization;

namespace AE.PID.Server.DTOs.PDMS;

public class DesignMaterialCategoryDto
{
    [JsonPropertyName("id")] public string Id { get; set; }

    /// <summary>
    ///     分类名称
    /// </summary>
    [JsonPropertyName("categoryName")]
    public string CategoryName { get; set; }

    /// <summary>
    /// 分类编码
    /// </summary>
    [JsonPropertyName("code")]
    public string Code { get; set; }
    
    /// <summary>
    ///    当前级别的编码
    /// </summary>
    [JsonPropertyName("categoryCode")]
    public string CategoryCode { get; set; }

    /// <summary>
    ///     上级分类
    /// </summary>
    [JsonPropertyName("parentId")]
    public string ParentId { get; set; }

    /// <summary>
    ///     显示顺序
    /// </summary>
    [JsonPropertyName("showOrder")]
    public string ShowOrder { get; set; }
}