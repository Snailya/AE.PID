using System.Text.Json.Serialization;

namespace AE.PID.Server.PDMS;

public class DesignMaterialAttributeDto
{
    [JsonPropertyName("id")] public string Id { get; set; }

    [JsonPropertyName("materialAttributeName")]
    public string Name { get; set; }

    [JsonPropertyName("materialAttributeValue")]
    public string Value { get; set; }
}