using System.Collections.Generic;

namespace AE.PID.Core;

public class MaterialDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public int[] Categories { get; set; } = [];
    public string Brand { get; set; } = string.Empty;
    public string Specifications { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public string Manufacturer { get; set; } = string.Empty;
    public string ManufacturerMaterialNumber { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public IEnumerable<MaterialPropertyDto> Properties { get; set; } = [];

    public string TechnicalDataEnglish { get; set; } = string.Empty;
    public string TechnicalData { get; set; } = string.Empty;
    public string Classification { get; set; } = string.Empty;
    public string Attachment { get; set; } = string.Empty;
}

public class MaterialPropertyDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}