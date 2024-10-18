using System.Text.Json.Serialization;

namespace AE.PID.Server.DTOs.PDMS;

public class ProcessSectionDto
{
    [JsonPropertyName("id")] public string Id { get; set; } = string.Empty;

    [JsonPropertyName("deviceTypeChineseName")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("deviceTypeEnglishName")]
    public string EnglishName { get; set; } = string.Empty;

    [JsonPropertyName("deviceTypeCode")] public string Code { get; set; } = string.Empty;
    [JsonPropertyName("showOrder")] public decimal Index { get; set; }
    [JsonPropertyName("usefullOrNot")] public string IsEnabled { get; set; } = string.Empty;
}