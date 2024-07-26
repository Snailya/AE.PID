namespace AE.PID.Server.DTOs.PDMS;

public class SelectDesignMaterialRequestDto : PagedRequestDto<DesignMaterialDto>
{
    public string GetQuery()
    {
        return $"MaterialName: {MainTable.MaterialName}, " +
               $"MaterialCode: {MainTable.MaterialCode}, " +
               $"Model: {MainTable.Model}, " +
               $"MaterialCategory: {MainTable.MaterialCategory}," +
               $"Brand: {MainTable.Brand}," +
               $"Specifications: {MainTable.Specifications}," +
               $"Manufacturer: {MainTable.Manufacturer}";
    }
}