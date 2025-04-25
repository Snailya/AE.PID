using System.Collections.Generic;
using System.ComponentModel;

namespace AE.PID.Core;

public class MaterialDto
{
    [Description("ID")] public int Id { get; set; }
    [Description("名称")] public string Name { get; set; } = string.Empty;
    [Description("编码")] public string Code { get; set; } = string.Empty;
    [Description("分类")] public int[] Categories { get; set; } = [];
    [Description("品牌")] public string Brand { get; set; } = string.Empty;
    public string Specifications { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    [Description("单位")] public string Unit { get; set; } = string.Empty;
    [Description("制造商")] public string Manufacturer { get; set; } = string.Empty;
    [Description("制造商物料编码")] public string ManufacturerMaterialNumber { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    [Description("描述")] public string Description { get; set; } = string.Empty;
    [Description("自定义属性")] public IEnumerable<MaterialPropertyDto> Properties { get; set; } = [];

    public string TechnicalDataEnglish { get; set; } = string.Empty;
    public string TechnicalData { get; set; } = string.Empty;
    public string Classification { get; set; } = string.Empty;
    public string Attachment { get; set; } = string.Empty;
}

public class MaterialPropertyDto
{
    [Description("ID")]public string Id { get; set; } = string.Empty;
    [Description("属性名")]public string Name { get; set; } = string.Empty;
    [Description("属性值")] public string Value { get; set; } = string.Empty;
}