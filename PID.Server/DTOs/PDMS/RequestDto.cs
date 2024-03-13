using System.Text.Json.Serialization;
using AE.PID.Server.Services;

namespace AE.PID.Server.DTOs.PDMS;

public class RequestDto<T> : RequestDto
{
    [JsonPropertyName("mainTable")] public T MainTable { get; set; }
}

public class RequestDto
{
    // ReSharper disable once StringLiteralTypo
    [JsonPropertyName("operationinfo")] public OperationInfoDto OperationInfo { get; set; }
    [JsonPropertyName("header")] public HeaderDto Header { get; } = ApiHelper.CreateHeader();
}