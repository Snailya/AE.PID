using System.Text.Json.Serialization;

namespace AE.PID.Server.DTOs.PDMS;

public class DesignMaterialDto
{
    [JsonPropertyName("id")] 
    public string Id { get; set; }

    /// <summary>
    /// 物料名称
    /// </summary>
    [JsonPropertyName("materialName")]
    public string MaterialName { get; set; }

    /// <summary>
    /// 物料编码
    /// </summary>
    [JsonPropertyName("materialCode")]
    public string MaterialCode { get; set; }

    /// <summary>
    /// 物料分类
    /// </summary>
    [JsonPropertyName("materialCategory")]
    public string MaterialCategory { get; set; }

    /// <summary>
    /// 显示顺序
    /// </summary>
    [JsonPropertyName("showOrder")]
    public string ShowOrder { get; set; }

    /// <summary>
    /// 品牌
    /// </summary>
    [JsonPropertyName("brand")]
    public string Brand { get; set; }

    /// <summary>
    /// 规格
    /// </summary>
    [JsonPropertyName("specifications")]
    public string Specifications { get; set; }

    /// <summary>
    /// 型号
    /// </summary>
    [JsonPropertyName("model")]
    public string Model { get; set; }

    /// <summary>
    /// 计量单位
    /// </summary>
    [JsonPropertyName("unit")]
    public string Unit { get; set; }

    /// <summary>
    /// 制造商
    /// </summary>
    [JsonPropertyName("manufacturer")]
    public string Manufacturer { get; set; }

    /// <summary>
    /// 制造商物料号
    /// </summary>
    [JsonPropertyName("manufacturerMaterialNumber")]
    public string ManufacturerMaterialNumber { get; set; }

    /// <summary>
    /// 物料类别，0-易损件；1-备件；2-中间件；3-组件
    /// </summary>
    [JsonPropertyName("materialType")]
    public string MaterialType { get; set; }
    
    /// <summary>
    /// 说明
    /// </summary>
    [JsonPropertyName("description")]
    public string Description { get; set; }
}