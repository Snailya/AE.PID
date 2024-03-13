using System.Collections;
using System.Text.Json.Serialization;

namespace AE.PID.Server.DTOs.PDMS;

public class SelectDesignMaterialResponseItemDto : ResponseItem<DesignMaterialDto>
{
    [JsonPropertyName("detail1")] public IEnumerable<DesignMaterialAttributeDto> Detail1 { get; set; }
    [JsonPropertyName("detail2")] public IEnumerable Detail2 { get; set; }
    [JsonPropertyName("detail3")] public IEnumerable Detail3 { get; set; }
    [JsonPropertyName("detail4")] public IEnumerable Detail4 { get; set; }
}