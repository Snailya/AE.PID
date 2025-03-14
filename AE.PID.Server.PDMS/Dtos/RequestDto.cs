﻿using System.Text.Json.Serialization;

namespace AE.PID.Server.PDMS;

public class RequestDto<T> : RequestDto
{
    [JsonPropertyName("mainTable")] public T MainTable { get; set; }
}

public class RequestDto
{
    // ReSharper disable once StringLiteralTypo
    [JsonPropertyName("operationinfo")] public OperationInfoDto OperationInfo { get; set; }
    [JsonPropertyName("header")] public HeaderDto Header { get; } = PDMSApiResolver.CreateHeader();
}

public class BipRequestDto<T>
{
    [JsonPropertyName("body")] public T Body { get; set; }
    [JsonPropertyName("head")] public BipHeaderDto Header { get; set; }
}