using System.Text.Json.Serialization;

namespace AE.PID.Server.DTOs.PDMS;

public class PageInfoDto(int pageNo, int pageSize)
{
    [JsonPropertyName("pageNo")] public int PageNo { get; private set; } = pageNo;
    [JsonPropertyName("pageSize")] public int PageSize { get; private set; } = pageSize;
}