using System.Text.Json.Serialization;

namespace AE.PID.Dtos;

public class DesignMaterialDto
{
    [JsonPropertyName("id")] public string Id { get; set; }

    /// <summary>
    ///     物料名称
    /// </summary>
    [JsonPropertyName("materialName")]
    public string MaterialName { get; set; } = string.Empty;

    /// <summary>
    ///     物料编码
    /// </summary>
    [JsonPropertyName("materialCode")]
    public string MaterialCode { get; set; } = string.Empty;

    /// <summary>
    ///     物料分类
    /// </summary>
    [JsonPropertyName("materialCategory")]
    public string MaterialCategory { get; set; } = string.Empty;

    /// <summary>
    ///     显示顺序
    /// </summary>
    [JsonPropertyName("showOrder")]
    public string ShowOrder { get; set; } = string.Empty;

    /// <summary>
    ///     品牌
    /// </summary>
    [JsonPropertyName("brand")]
    public string Brand { get; set; } = string.Empty;

    /// <summary>
    ///     规格
    /// </summary>
    [JsonPropertyName("specifications")]
    public string Specifications { get; set; } = string.Empty;

    /// <summary>
    ///     型号
    /// </summary>
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    /// <summary>
    ///     计量单位
    /// </summary>
    [JsonPropertyName("unit")]
    public string Unit { get; set; } = string.Empty;

    /// <summary>
    ///     制造商
    /// </summary>
    [JsonPropertyName("manufacturer")]
    public string Manufacturer { get; set; } = string.Empty;

    /// <summary>
    ///     制造商物料号
    /// </summary>
    [JsonPropertyName("manufacturerMaterialNumber")]
    public string ManufacturerMaterialNumber { get; set; } = string.Empty;

    /// <summary>
    ///     物料类别，0-易损件；1-备件；2-中间件；3-组件
    /// </summary>
    [JsonPropertyName("materialType")]
    public string MaterialType { get; set; } = string.Empty;

    /// <summary>
    ///     说明
    /// </summary>
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;
}