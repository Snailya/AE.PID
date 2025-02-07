using System.Text.Json.Serialization;

namespace AE.PID.Server.PDMS;

public class OperationInfoDto
{
    [JsonPropertyName("operator")] public string Operator { get; set; }
}