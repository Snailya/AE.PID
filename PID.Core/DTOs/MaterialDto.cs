namespace AE.PID.Core.DTOs;

public class MaterialDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Code { get; set; }
    public string[] Categories { get; set; }
    public string Brand { get; set; }
    public string Specifications { get; set; }
    public string Model { get; set; }
    public string Unit { get; set; }
    public string Manufacturer { get; set; }
    public string ManufacturerMaterialNumber { get; set; }
    public int Type { get; set; }
    public int State { get; set; }
    public string Description { get; set; }
    public IEnumerable<MaterialPropertyDto> Properties { get; set; }
}