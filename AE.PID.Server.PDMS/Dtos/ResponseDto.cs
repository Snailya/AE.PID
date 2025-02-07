using System.Text.Json.Serialization;

namespace AE.PID.Server.PDMS;

public class ResponseDto
{
    [JsonPropertyName("result")] public string Result { get; set; }
}