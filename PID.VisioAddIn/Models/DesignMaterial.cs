using System.Collections.Generic;
using System.Linq;
using AE.PID.Attributes;
using AE.PID.Dtos;
using Newtonsoft.Json;

namespace AE.PID.Models;

/// <summary>
///     A design material is used for design bom which might pair to one or many erp material in a purchase system.
/// </summary>
/// <param name="name"></param>
/// <param name="categories"></param>
public class DesignMaterial(
    string materialNo,
    string name,
    string brand,
    string specifications,
    string type,
    string unit,
    string supplier,
    string manufacturerMaterialNumber,
    string technicalData,
    string technicalDataEnglish,
    int[] categories)
{
    /// <summary>
    ///     A design material could belong to several categories on different level, for example, a pump from pump, gear pump
    ///     category.
    /// </summary>
    public int[] Categories { get; set; } = categories;

    /// <summary>
    ///     The extra properties used as technical data
    /// </summary>
    [DataGridMultipleColumns]
    public List<DesignMaterialProperty> Properties { get; set; } = [];

    public static DesignMaterial FromDTO(MaterialDto dto)
    {
        return new DesignMaterial(dto.Code, dto.Name, dto.Brand, dto.Specifications, dto.Model, dto.Unit,
            dto.Manufacturer, dto.ManufacturerMaterialNumber, string.Empty, string.Empty, dto.Categories)
        {
            Properties = dto.Properties.Any() ? dto.Properties.Select(DesignMaterialProperty.FromDTO).ToList() : []
        };
    }

    #region General Properties

    /// <summary>
    ///     The human-readable bom code from a database, used as identity
    /// </summary>
    [DataGridColumnName("物料号")]
    [JsonProperty("a")]
    public string MaterialNo { get; private set; } = materialNo;

    /// <summary>
    ///     The display text of the design material
    /// </summary>
    [DataGridColumnName("名称")]
    [JsonProperty("b")]
    public string Name { get; private set; } = name;

    /// <summary>
    ///     The brand of this material. A manufacturer could have many brands.
    /// </summary>
    [DataGridColumnName("品牌")]
    [JsonProperty("c")]
    public string Brand { get; set; } = brand;

    [JsonProperty("d")]
    [DataGridColumnName("规格")]
    public string Specifications { get; set; } = specifications;

    [JsonProperty("e")]
    [DataGridColumnName("型号")]
    public string Type { get; set; } = type;

    [JsonProperty("f")]
    [DataGridColumnName("单位")]
    public string Unit { get; set; } = unit;

    [JsonProperty("g")]
    [DataGridColumnName("供应商")]
    public string Supplier { get; set; } = supplier;

    [JsonProperty("h")]
    [DataGridColumnName("制造商物料号")]
    public string ManufacturerMaterialNumber { get; set; } = manufacturerMaterialNumber;

    [JsonProperty("i")]
    [DataGridColumnName("技术参数-英文")]
    public string TechnicalDataEnglish { get; set; } = technicalData;

    [JsonProperty("j")]
    [DataGridColumnName("技术参数-中文")]
    public string TechnicalData { get; set; } = technicalDataEnglish;

    #endregion
}