namespace AE.PID.Client.Core;

public class Material
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public MaterialCategory Category { get; set; } = null!;
    public MaterialProperty[] Properties { get; set; } = [];
    public string Brand { get; set; } = string.Empty;
    public string Specifications { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public string Supplier { get; set; } = string.Empty;
    public string ManufacturerMaterialNumber { get; set; } = string.Empty;
    public string TechnicalDataEnglish { get; set; } = string.Empty;
    public string TechnicalData { get; set; } = string.Empty;
    public string Classification { get; set; } = string.Empty;
    public string Attachment { get; set; } = string.Empty;
}