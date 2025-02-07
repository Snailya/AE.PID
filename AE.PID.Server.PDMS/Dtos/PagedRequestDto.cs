using System.Text.Json.Serialization;

namespace AE.PID.Server.PDMS;

public class PagedRequestDto<T> : RequestDto<T>
{
    [JsonPropertyName("pageInfo")] public PageInfoDto PageInfo { get; set; }
}