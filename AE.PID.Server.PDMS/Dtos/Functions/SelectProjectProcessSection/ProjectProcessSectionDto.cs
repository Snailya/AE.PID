using System.Text.Json.Serialization;

namespace AE.PID.Server.PDMS;

public class ProjectProcessSectionDto
{
    [JsonPropertyName("id")] public string Id { get; set; }
    [JsonPropertyName("projectId")] public string ProjectId { get; set; }

    [JsonPropertyName("functionGroupChineseName")]
    public string Name { get; set; }

    [JsonPropertyName("functionGroupEnglishName")]
    public string EnglishName { get; set; }

    [JsonPropertyName("functionGroupCode")]
    public string Code { get; set; }

    [JsonPropertyName("workshop")] public string Workshop { get; set; }
    [JsonPropertyName("workshopLine")] public string WorkshopLine { get; set; }
    [JsonPropertyName("sectionName")] public string SectionName { get; set; }
    [JsonPropertyName("sectionLine")] public string SectionLine { get; set; }
    [JsonPropertyName("deviceTypeName")] public string DeviceTypeName { get; set; }
    [JsonPropertyName("deviceNumber")] public string DeviceNumber { get; set; }
    [JsonPropertyName("usefullOrNot")] public string IsEnabled { get; set; } = string.Empty;

    [JsonPropertyName("functionGroupStatus")]
    public string FunctionGroupStatus { get; set; }

    [JsonPropertyName("chargeMember")] public string ChargeMember { get; set; }
    [JsonPropertyName("description")] public string Description { get; set; }

    [JsonPropertyName("standardProcessArea")]
    public string StandardProcessArea { get; set; }
}