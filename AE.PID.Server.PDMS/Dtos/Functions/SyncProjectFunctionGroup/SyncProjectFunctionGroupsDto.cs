using System.Text.Json.Serialization;

namespace AE.PID.Server.PDMS;

public class SyncProjectFunctionGroupsDto
{
    [JsonPropertyName("projectId")] public string ProjectId { get; set; } = string.Empty;

    [JsonPropertyName("projectProcessAreaId")]
    public string ZoneId { get; set; } = string.Empty;

    [JsonPropertyName("userCode")] public string UserId { get; set; } = string.Empty;

    [JsonPropertyName("deviceId")] public string DeviceId { get; set; } = string.Empty;

    [JsonPropertyName("functionGroupList")]
    public List<SyncProjectFunctionGroupItemDto> Items { get; set; } = [];
}

public class SyncProjectFunctionGroupItemDto
{
    [JsonPropertyName("projectFunctionGroup")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("serialNumber")] public string Number { get; set; } = string.Empty;

    [JsonPropertyName("usefullOrNot")] public bool IsEnabled { get; set; }

    [JsonPropertyName("functionGroupId")] public string TemplatedId { get; set; } = string.Empty;
}