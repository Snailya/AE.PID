using System.Text.Json.Serialization;

namespace AE.PID.Server.DTOs.PDMS;

public class SelectDesignMaterialResponseItemDto : ResponseItem<DesignMaterialDto>
{
    [JsonPropertyName("detail1")] public IEnumerable<DesignMaterialAttributeDto> Detail1 { get; set; }
}