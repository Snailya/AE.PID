using System.Text.Json.Serialization;

namespace AE.PID.Server.PDMS;

public class FunctionGroupDto
{
    [JsonPropertyName("id")] public string Id { get; set; } = string.Empty;

    [JsonPropertyName("groupName")] public string Category { get; set; } = string.Empty;

    [JsonPropertyName("functionGroupChineseName")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("functionGroupEnglishName")]
    public string EnglishName { get; set; } = string.Empty;

    [JsonPropertyName("functionGroupCode")]
    public string Code { get; set; } = string.Empty;

    [JsonPropertyName("functionGroupSimpleWord")]
    public string Abbreviation { get; set; } = string.Empty;

    [JsonPropertyName("usefullOrNot")] public string IsEnabled { get; set; } = string.Empty;

    [JsonPropertyName("processSectionId")] public string ZoneId { get; set; } = string.Empty;

    [JsonPropertyName("deviceTypeId")] public string DeviceId { get; set; } = string.Empty;
}