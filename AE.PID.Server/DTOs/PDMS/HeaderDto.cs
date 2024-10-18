using System.Text.Json.Serialization;

namespace AE.PID.Server.DTOs.PDMS;

public class HeaderDto
{
    [JsonPropertyName("systemid")] public string SystemId { get; set; } = string.Empty;
    [JsonPropertyName("currentDateTime")] public string Time { get; set; } = string.Empty;
    [JsonPropertyName("Md5")] public string MD5 { get; set; } = string.Empty;
}