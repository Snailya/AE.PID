using System.Text.Json.Serialization;

namespace AE.PID.Server.DTOs.PDMS;

public class HeaderDto
{
    [JsonPropertyName("systemid")] public string SystemId { get; set; }
    [JsonPropertyName("currentDateTime")] public string CurrentDateTime { get; set; }
    [JsonPropertyName("Md5")] public string MD5 { get; set; }
}