using System.Text.Json.Serialization;

namespace AE.PID.Server.DTOs.PDMS;

public class ResponseItem<T>
{
    [JsonPropertyName("mainTable")] public T MainTable { get; set; }
}