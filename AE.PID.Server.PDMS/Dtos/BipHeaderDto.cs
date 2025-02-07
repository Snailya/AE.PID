using System.Text.Json.Serialization;

namespace AE.PID.Server.PDMS;

public class BipHeaderDto
{
    [JsonPropertyName("requestUserId")] public string UserInternalId { get; set; } = string.Empty;

    [JsonPropertyName("requestUserCode")] public string UserId { get; set; } = string.Empty;

    [JsonPropertyName("requestDeviceId")] public string UUID { get; set; } = string.Empty;

    [JsonPropertyName("requestDateTime")] public string Time { get; set; } = string.Empty;

    [JsonPropertyName("requestId")] public string Id { get; set; } = string.Empty;

    [JsonPropertyName("bipCode")] public string BipCode { get; set; } = string.Empty;

    [JsonPropertyName("fromSystemCode")] public string FromSystemCode { get; set; } = "Vosio";

    [JsonPropertyName("toSystemCode")] public string ToSystemCode { get; set; } = "Weaver";
}