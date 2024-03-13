using System.Text.Json.Serialization;

namespace AE.PID.Server.DTOs.PDMS;

public class DesignMaterialAttributeDto
{
    [JsonPropertyName("materialAttributeId")]
    public string Id { get; set; }

    [JsonPropertyName("materialAttributeName")]
    public string Name { get; set; }

    [JsonPropertyName("materialAttributeValue")]
    public string Value { get; set; }
}