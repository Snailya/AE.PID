using System.Text.Json.Serialization;

namespace AE.PID.Server.DTOs.PDMS;

public class PageCountDto
{
    [JsonPropertyName("pageCount")] public int PageCount { get; set; }
}