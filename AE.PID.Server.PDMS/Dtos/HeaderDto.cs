using System.ComponentModel;
using System.Text.Json.Serialization;

namespace AE.PID.Server.PDMS;

public class HeaderDto
{
    [JsonPropertyName("systemid")] public string SystemId { get; set; } = string.Empty;

    [Description("时间戳")]
    [JsonPropertyName("currentDateTime")]
    public string Time { get; set; } = string.Empty;

    [JsonPropertyName("Md5")] public string MD5 { get; set; } = string.Empty;
}